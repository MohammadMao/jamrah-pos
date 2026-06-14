using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using JamrahPOS.Models;

namespace JamrahPOS.Services
{
    // ─────────────────────────────────────────────
    // Internal models
    // ─────────────────────────────────────────────

    internal enum LineAlign { Right, Center, Left }

    internal enum LineKind
    {
        Text,
        ThickDivider,   // solid bold rule
        ThinDivider,    // dashed rule
        Space           // blank vertical gap
    }

    internal sealed class DrawLine
    {
        public LineKind Kind { get; set; } = LineKind.Text;
        public string Text { get; set; } = "";
        public float FontSize { get; set; } = 22f;
        public bool Bold { get; set; }
        public LineAlign Align { get; set; } = LineAlign.Right;
        public int IndentPx { get; set; }  // extra left-side inset (appears on right for RTL)
        public int SpaceHeight { get; set; } = 10; // used only when Kind == Space
    }

    // ─────────────────────────────────────────────
    // Renderer
    // ─────────────────────────────────────────────

    /// <summary>
    /// Renders receipt / report content to an ESC/POS raster byte array.
    ///
    /// Approach:
    ///   1. Build a list of DrawLine instructions.
    ///   2. Measure each line's height using a scratch Graphics context.
    ///   3. Create a Bitmap of the required total height.
    ///   4. Draw every instruction onto the bitmap using GDI+ (handles Arabic BiDi correctly).
    ///   5. Convert the 32-bpp bitmap to 1-bpp ESC/POS GS v 0 raster data.
    ///   6. Wrap with ESC @ (init) and GS V (feed + partial cut) commands.
    /// </summary>
    internal sealed class ReceiptRenderer
    {
        // ── XP-K200L on 80 mm paper ──────────────────────────────────────────
        // 8 dots/mm × 72 mm printable width ≈ 576 dots
        private const int PRINT_WIDTH = 576;
        private const int H_PADDING   = 14;   // horizontal padding each side
        private const int TEXT_WIDTH  = PRINT_WIDTH - H_PADDING * 2;
        private const int LINE_GAP    = 4;    // extra vertical gap after each line

        // Font sizes (pixels)
        private const float SZ_XLARGE = 34f;
        private const float SZ_NORMAL = 23f;
        private const float SZ_SMALL  = 18f;

        private const string FONT_NAME = "Tahoma"; // excellent Arabic support on Windows

        // ── ESC/POS command bytes ────────────────────────────────────────────
        private static readonly byte[] CMD_INIT     = { 0x1B, 0x40 };            // ESC @  – init printer
        private static readonly byte[] CMD_FEED_CUT = { 0x1B, 0x64, 0x05,       // ESC d 5 – feed 5 lines
                                                         0x1D, 0x56, 0x42, 0x06 }; // GS V B 6 – partial cut

        // ════════════════════════════════════════════════════════════════
        // Public entry-points
        // ════════════════════════════════════════════════════════════════

        /// <summary>Render an Order receipt to ESC/POS bytes.</summary>
        public byte[] RenderOrderReceipt(Order order, User cashier)
            => RenderToEscPos(BuildOrderLines(order, cashier));

        /// <summary>Render a kitchen copy (items + quantities only, no prices).</summary>
        public byte[] RenderKitchenReceipt(Order order)
            => RenderToEscPos(BuildKitchenLines(order));

        /// <summary>Render a pre-built report text block to ESC/POS bytes.</summary>
        public byte[] RenderReportText(IEnumerable<string> reportLines)
            => RenderToEscPos(BuildReportLines(reportLines));

        // ════════════════════════════════════════════════════════════════
        // Line builders
        // ════════════════════════════════════════════════════════════════

        private static List<DrawLine> BuildOrderLines(Order order, User cashier)
        {
            var L = new List<DrawLine>();

            var shortNum = PrintService.GetShortOrderNumber(order.OrderNumber);

            // ── Header ──────────────────────────────────────────────────
            L.Add(Thick());
            L.Add(Text("مطعم جمرة",         SZ_XLARGE, bold: true,  align: LineAlign.Center));
            L.Add(Text("أصل الطعم المشوي",  SZ_NORMAL, align: LineAlign.Center));
            L.Add(Text("الخرطوم بحري",      SZ_NORMAL, align: LineAlign.Center));
            L.Add(Text("هاتف: 0912147130",  SZ_NORMAL, align: LineAlign.Center));
            L.Add(Thick());
            L.Add(Space(8));

            // ── Order meta ──────────────────────────────────────────────
            L.Add(Text("رقم الطلب:", SZ_NORMAL, bold: true, align: LineAlign.Center));
            L.Add(Text(shortNum, SZ_XLARGE, bold: true, align: LineAlign.Center));
            L.Add(Thin());
            L.Add(Text($"التاريخ:   {order.OrderDateTime:yyyy/MM/dd}",  SZ_NORMAL));
            L.Add(Text($"الوقت:     {order.OrderDateTime:HH:mm:ss}",    SZ_NORMAL));
            L.Add(Text($"الكاشير:   {cashier.Username}",                SZ_NORMAL));
            L.Add(Text($"الدفع:     {order.PaymentMethod}",             SZ_NORMAL));
            L.Add(Text($"نوع الطلب:  {order.OrderType}",               SZ_NORMAL));
            L.Add(Thick());
            L.Add(Space(6));

            // ── Items ───────────────────────────────────────────────────
            L.Add(Text("الأصناف:", SZ_NORMAL, bold: true));
            L.Add(Thin());

            foreach (var item in order.OrderItems)
            {
                var name = item.MenuItem?.Name ?? "صنف";
                L.Add(Text($"• {name}", SZ_NORMAL, bold: true));
                L.Add(Text($"الكمية: {item.Quantity}   |   السعر: {item.UnitPrice:N2}   |   المجموع: {item.TotalPrice:N2}",
                            SZ_SMALL, indent: 20));

                if (item.MenuItem != null && item.UnitPrice < item.MenuItem.Price)
                {
                    var disc = (item.MenuItem.Price - item.UnitPrice) * item.Quantity;
                    L.Add(Text($"خصم: {disc:N2} SDG", SZ_SMALL, indent: 20));
                }
                L.Add(Space(5));
            }

            L.Add(Thin());
            L.Add(Space(4));

            // ── Totals ──────────────────────────────────────────────────
            var totalDiscount = order.OrderItems
                .Where(i => i.MenuItem != null)
                .Sum(i => (i.MenuItem!.Price - i.UnitPrice) * i.Quantity);

            if (totalDiscount > 0)
            {
                L.Add(Text($"قبل الخصم: {order.TotalAmount + totalDiscount:N2} SDG", SZ_NORMAL));
                L.Add(Text($"الخصم:     {totalDiscount:N2} SDG",                     SZ_NORMAL));
            }

            L.Add(Text($"الإجمالي: {order.TotalAmount:N2} SDG", SZ_XLARGE, bold: true));
            L.Add(Space(8));

            // ── Footer ──────────────────────────────────────────────────
            L.Add(Thick());
            L.Add(Text("شكراً لزيارتكم",        SZ_NORMAL, align: LineAlign.Center));
            L.Add(Text("نتمنى لكم تجربة ممتعة", SZ_NORMAL,  align: LineAlign.Center));
            L.Add(Thick());
            L.Add(Space(16));

            return L;
        }

        private static List<DrawLine> BuildKitchenLines(Order order)
        {
            var L = new List<DrawLine>();

            var shortNum = PrintService.GetShortOrderNumber(order.OrderNumber);

            // ── Header ──────────────────────────────────────────────────
            L.Add(Thick());
            L.Add(Text("مطعم جمرة",        SZ_NORMAL, bold: true, align: LineAlign.Center));
            L.Add(Text("الخرطوم بحري",     SZ_SMALL,  align: LineAlign.Center));
            L.Add(Text("هاتف: 0912147130", SZ_SMALL,  align: LineAlign.Center));
            L.Add(Thick());
            L.Add(Space(6));

            // ── Order meta ───────────────────────────────────────────────
            L.Add(Text($"رقم الطلب: {shortNum}", SZ_NORMAL, bold: true));
            L.Add(Text($"نوع الطلب: {order.OrderType}",           SZ_SMALL));
            L.Add(Text($"التاريخ:   {order.OrderDateTime:yyyy/MM/dd}", SZ_SMALL));
            L.Add(Text($"الوقت:     {order.OrderDateTime:HH:mm:ss}",   SZ_SMALL));
            L.Add(Thin());
            L.Add(Space(6));

            // ── Items (name + quantity + prices) ─────────────────────────────
            foreach (var item in order.OrderItems)
            {
                var name = item.MenuItem?.Name ?? "صنف";
                L.Add(Text($"• {name}", SZ_NORMAL, bold: true));
                L.Add(Text($"الكمية: {item.Quantity}   السعر: {item.UnitPrice:N2}   المجموع: {item.TotalPrice:N2}", SZ_SMALL, indent: 20));
                L.Add(Space(5));
            }

            L.Add(Thin());
            L.Add(Space(4));

            // ── Total ────────────────────────────────────────────────────
            L.Add(Text($"الإجمالي: {order.TotalAmount:N2} SDG", SZ_NORMAL, bold: true));
            L.Add(Space(8));

            // ── Footer ──────────────────────────────────────────────────
            L.Add(Thick());
            L.Add(Text("شكراً لزيارتكم",        SZ_NORMAL, align: LineAlign.Center));
            L.Add(Text("نتمنى لكم تجربة ممتعة", SZ_NORMAL, align: LineAlign.Center));
            L.Add(Thick());
            L.Add(Space(16));

            return L;
        }

        private static List<DrawLine> BuildReportLines(IEnumerable<string> reportLines)
        {
            var L = new List<DrawLine>();

            foreach (var raw in reportLines)
            {
                var line = raw.TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(line))
                {
                    L.Add(Space(8));
                }
                else if (line.StartsWith("====") || line.StartsWith("════"))
                {
                    L.Add(Thick());
                }
                else if (line.StartsWith("----") || line.StartsWith("────"))
                {
                    L.Add(Thin());
                }
                else
                {
                    // Detect section headers (lines ending with ':')
                    bool isHeader = line.TrimEnd().EndsWith(':') && !line.Contains(' ') == false
                                    && line.Length < 30;
                    L.Add(Text(line, SZ_NORMAL, bold: isHeader));
                }
            }

            L.Add(Space(16));
            return L;
        }

        // ════════════════════════════════════════════════════════════════
        // Render pipeline
        // ════════════════════════════════════════════════════════════════

        private byte[] RenderToEscPos(List<DrawLine> lines)
        {
            // ── Pass 1: measure total height ────────────────────────────
            using var dummy = new Bitmap(1, 1);
            using var mg    = Graphics.FromImage(dummy);

            var heights = new int[lines.Count];
            int totalH  = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                int h = MeasureLineHeight(mg, lines[i]);
                heights[i] = h;
                totalH    += h;
            }

            // ── Pass 2: draw onto bitmap ────────────────────────────────
            using var bmp = new Bitmap(PRINT_WIDTH, Math.Max(totalH, 1), PixelFormat.Format32bppArgb);
            using var g   = Graphics.FromImage(bmp);

            g.Clear(Color.White);
            g.SmoothingMode      = SmoothingMode.HighQuality;
            g.TextRenderingHint  = TextRenderingHint.AntiAliasGridFit;

            int y = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                DrawInstruction(g, lines[i], y, heights[i]);
                y += heights[i];
            }

            // ── Pass 3: bitmap → ESC/POS bytes ──────────────────────────
            return BitmapToEscPos(bmp);
        }

        // ════════════════════════════════════════════════════════════════
        // Measure helpers
        // ════════════════════════════════════════════════════════════════

        private static int MeasureLineHeight(Graphics g, DrawLine line)
        {
            return line.Kind switch
            {
                LineKind.Space        => line.SpaceHeight,
                LineKind.ThickDivider => 10 + LINE_GAP,
                LineKind.ThinDivider  =>  8 + LINE_GAP,
                _ => (int)Math.Ceiling(MeasureTextHeight(g, line)) + LINE_GAP
            };
        }

        private static float MeasureTextHeight(Graphics g, DrawLine line)
        {
            using var font = GetFont(line);
            var sf = BuildStringFormat(line.Align);
            // Measure the actual text so multi-line wrapping is accounted for.
            // Fall back to a dummy string only when there is no real content.
            var textToMeasure = string.IsNullOrEmpty(line.Text) ? "أبجدABCDpg" : line.Text;
            var sz = g.MeasureString(textToMeasure, font, TEXT_WIDTH - line.IndentPx, sf);
            return sz.Height;
        }

        // ════════════════════════════════════════════════════════════════
        // Draw helpers
        // ════════════════════════════════════════════════════════════════

        private static void DrawInstruction(Graphics g, DrawLine line, int y, int height)
        {

            switch (line.Kind)
            {
                case LineKind.ThickDivider:
                {
                    int my = y + height / 2;
                    using var pen = new Pen(Color.Black, 2f);
                    g.DrawLine(pen, H_PADDING, my, PRINT_WIDTH - H_PADDING, my);
                    break;
                }
                case LineKind.ThinDivider:
                {
                    int my = y + height / 2;
                    using var pen = new Pen(Color.Black, 1f) { DashStyle = DashStyle.Dash };
                    g.DrawLine(pen, H_PADDING, my, PRINT_WIDTH - H_PADDING, my);
                    break;
                }
                case LineKind.Space:
                    break; // intentionally blank

                default: // Text
                {
                    using var font = GetFont(line);
                    var sf   = BuildStringFormat(line.Align);
                    int left = H_PADDING + line.IndentPx;
                    int w    = TEXT_WIDTH - line.IndentPx;
                    var rect = new RectangleF(left, y, w, height);
                    g.DrawString(line.Text, font, Brushes.Black, rect, sf);
                    break;
                }
            }
        }

        // ── StringFormat: always RTL reading direction ──────────────────
        private static StringFormat BuildStringFormat(LineAlign align)
        {
            // DirectionRightToLeft makes "Near" = right edge, "Far" = left edge
            var sf = new StringFormat(StringFormatFlags.DirectionRightToLeft
                                    | StringFormatFlags.LineLimit);
            sf.LineAlignment = StringAlignment.Center;
            sf.Trimming      = StringTrimming.EllipsisCharacter;

            sf.Alignment = align switch
            {
                LineAlign.Center => StringAlignment.Center,
                LineAlign.Left   => StringAlignment.Far,   // Far in RTL = visual left
                _                => StringAlignment.Near,  // Near in RTL = visual right
            };
            return sf;
        }

        private static Font GetFont(DrawLine line)
            => new Font(FONT_NAME, line.FontSize,
                        line.Bold ? FontStyle.Bold : FontStyle.Regular,
                        GraphicsUnit.Pixel);

        // ════════════════════════════════════════════════════════════════
        // Bitmap → ESC/POS
        // ════════════════════════════════════════════════════════════════

        private static byte[] BitmapToEscPos(Bitmap bmp)
        {
            int width  = bmp.Width;
            int height = bmp.Height;
            int bpl    = (width + 7) / 8; // bytes per raster line

            // Lock and copy pixel data for fast access
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            int    stride     = bmpData.Stride;
            byte[] pixelBytes = new byte[stride * height];
            Marshal.Copy(bmpData.Scan0, pixelBytes, 0, pixelBytes.Length);
            bmp.UnlockBits(bmpData);

            using var ms = new MemoryStream();

            // ESC @ – initialize
            ms.Write(CMD_INIT);

            // Send the image in strips of MAX_STRIP rows.
            // Many printers / emulators reject a single GS v 0 that is too tall.
            const int MAX_STRIP = 255;

            for (int startRow = 0; startRow < height; startRow += MAX_STRIP)
            {
                int stripH = Math.Min(MAX_STRIP, height - startRow);

                // GS v 0 – print raster bit image
                ms.WriteByte(0x1D);
                ms.WriteByte(0x76);
                ms.WriteByte(0x30);
                ms.WriteByte(0x00);                            // m: normal density
                ms.WriteByte((byte)(bpl & 0xFF));              // xL
                ms.WriteByte((byte)((bpl >> 8) & 0xFF));       // xH
                ms.WriteByte((byte)(stripH & 0xFF));           // yL
                ms.WriteByte((byte)((stripH >> 8) & 0xFF));   // yH

                for (int row = startRow; row < startRow + stripH; row++)
                {
                    int rowBase = row * stride;
                    var lineBytes = new byte[bpl];

                    for (int col = 0; col < width; col++)
                    {
                        int  offset    = rowBase + col * 4;
                        byte b         = pixelBytes[offset];
                        byte gn        = pixelBytes[offset + 1];
                        byte r         = pixelBytes[offset + 2];
                        int  luminance = (r * 299 + gn * 587 + b * 114) / 1000;

                        if (luminance < 128)
                            lineBytes[col / 8] |= (byte)(0x80 >> (col % 8));
                    }

                    ms.Write(lineBytes);
                }
            }

            // Feed paper and partial cut
            ms.Write(CMD_FEED_CUT);

            return ms.ToArray();
        }

        // ════════════════════════════════════════════════════════════════
        // Factory helpers (keep builders concise)
        // ════════════════════════════════════════════════════════════════

        private static DrawLine Text(string text, float size,
            bool bold = false, LineAlign align = LineAlign.Right, int indent = 0)
            => new DrawLine { Kind = LineKind.Text, Text = text, FontSize = size,
                              Bold = bold, Align = align, IndentPx = indent };

        private static DrawLine Thick()
            => new DrawLine { Kind = LineKind.ThickDivider };

        private static DrawLine Thin()
            => new DrawLine { Kind = LineKind.ThinDivider };

        private static DrawLine Space(int px = 10)
            => new DrawLine { Kind = LineKind.Space, SpaceHeight = px };
    }
}
