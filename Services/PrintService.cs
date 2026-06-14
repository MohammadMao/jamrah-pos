using System.IO;
using System.Net.Sockets;
using System.Text;
using JamrahPOS.Helpers;
using JamrahPOS.Models;

namespace JamrahPOS.Services
{
    /// <summary>
    /// Handles all printing via ESC/POS commands sent directly to the thermal printer.
    /// </summary>
    public class PrintService
    {
        public static string PrinterName { get; set; } = "XP-80C (copy 1)";

        // ── Emulator mode ─────────────────────────────────────────────────────────
        // Set UseEmulator = true to send ESC/POS bytes over TCP to the Docker emulator
        // instead of the real Windows printer.  View results at http://localhost
        public static bool   UseEmulator  { get; set; } = false;
        public static string EmulatorHost { get; set; } = "127.0.0.1";
        public static int    EmulatorPort { get; set; } = 9100;

        // The Docker emulator (esc2html) is less tolerant than real printers for
        // large raster jobs. In emulator mode, prefer sending plain text.
        public static bool EmulatorTextMode { get; set; } = false;

        private static readonly byte[] CMD_INIT     = { 0x1B, 0x40 };                  // ESC @
        private static readonly byte[] CMD_FEED_CUT = { 0x1B, 0x64, 0x05,             // ESC d 5
                                 0x1D, 0x56, 0x42, 0x06 };    // GS V B 6

        private readonly ReceiptRenderer _renderer = new();

        /// <summary>
        /// Sends raw ESC/POS bytes either to the real printer (via winspool) or
        /// to the Docker emulator (via a plain TCP socket on port 9100).
        /// </summary>
        private static async Task<bool> SendBytesAsync(byte[] bytes)
        {
            if (UseEmulator)
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(EmulatorHost, EmulatorPort);
                    var stream = client.GetStream();
                    await stream.WriteAsync(bytes);
                    await stream.FlushAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PRINT] Emulator TCP error: {ex.Message}");
                    return false;
                }
            }

            return await Task.Run(() => RawPrinterHelper.SendBytesToPrinter(PrinterName, bytes));
        }

        private static byte[] BuildEscPosTextJob(string text)
        {
            // Normalise line endings to CRLF for ESC/POS parsers.
            // Prefix each line with RLM so the emulator's HTML/text renderer
            // displays Arabic in RTL direction more reliably.
            const string RLM = "\u200F";

            var normalized = text.Replace("\r\n", "\n");
            var lines = normalized.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                    lines[i] = RLM + lines[i];
            }

            var withRtl = string.Join("\r\n", lines);
            var payload = Encoding.UTF8.GetBytes(withRtl);

            using var ms = new MemoryStream();
            ms.Write(CMD_INIT);
            ms.Write(payload);
            ms.Write(CMD_FEED_CUT);
            return ms.ToArray();
        }

        // ════════════════════════════════════════════════════════════════
        // Order receipt
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Extracts the short daily sequence number from an OrderNumber like "20260302-5" → "5".
        /// Falls back to the full OrderNumber if the format is unexpected.
        /// </summary>
        public static string GetShortOrderNumber(string orderNumber)
        {
            var dash = orderNumber.LastIndexOf('-');
            return dash >= 0 ? orderNumber[(dash + 1)..] : orderNumber;
        }

        /// <summary>
        /// Renders and prints a full customer receipt using ESC/POS raster commands.
        /// Throws on rendering or send failure so callers can handle/display the error.
        /// </summary>
        public async Task<bool> PrintOrderAsync(Order order, User cashier)
        {
            if (UseEmulator && EmulatorTextMode)
            {
                var receiptText = GenerateReceipt(order, cashier);
                return await SendBytesAsync(BuildEscPosTextJob(receiptText));
            }

            var bytes = await Task.Run(() => _renderer.RenderOrderReceipt(order, cashier));
            return await SendBytesAsync(bytes);
        }

        /// <summary>
        /// Renders and prints a kitchen receipt (items + quantities only, no prices).
        /// Throws on rendering or send failure so callers can handle/display the error.
        /// </summary>
        public async Task<bool> PrintKitchenOrderAsync(Order order)
        {
            var bytes = await Task.Run(() => _renderer.RenderKitchenReceipt(order));
            return await SendBytesAsync(bytes);
        }

        // ════════════════════════════════════════════════════════════════
        // Report printing
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Renders and prints a report text block using ESC/POS raster commands.
        /// Throws on rendering or send failure so callers can handle/display the error.
        /// </summary>
        public async Task<bool> PrintReportAsync(string reportText)
        {
            if (UseEmulator && EmulatorTextMode)
                return await SendBytesAsync(BuildEscPosTextJob(reportText));

            var bytes = await Task.Run(() => _renderer.RenderReportText(reportText.Split('\n')));
            return await SendBytesAsync(bytes);
        }

        // ════════════════════════════════════════════════════════════════
        // File saving (kept for records / fallback)
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Generates a plain-text receipt string used for saving to file.
        /// </summary>
        public string GenerateReceipt(Order order, User cashier)
        {
            var sb = new StringBuilder();

            sb.AppendLine("================");
            sb.AppendLine("مطعم جمرة");
            sb.AppendLine("أصل الطعم المشوي");
            sb.AppendLine("الخرطوم بحري");
            sb.AppendLine("هاتف: 0912147130");
            sb.AppendLine("================");
            sb.AppendLine();

            sb.AppendLine("*** رقم الطلب ***");
            sb.AppendLine($"{GetShortOrderNumber(order.OrderNumber)}");
            sb.AppendLine("================");
            sb.AppendLine($"التاريخ: {order.OrderDateTime:yyyy/MM/dd}");
            sb.AppendLine($"الوقت: {order.OrderDateTime:HH:mm:ss}");
            sb.AppendLine($"الكاشير: {cashier.Username}");
            sb.AppendLine($"طريقة الدفع: {order.PaymentMethod}");
            sb.AppendLine($"نوع الطلب: {order.OrderType}");
            sb.AppendLine("================");
            sb.AppendLine();

            sb.AppendLine("الأصناف:");
            sb.AppendLine(new string('-', 32));

            foreach (var item in order.OrderItems)
            {
                var name = item.MenuItem?.Name ?? "صنف";
                sb.AppendLine($"• {name}");
                sb.AppendLine($"  الكمية: {item.Quantity}  السعر: {item.UnitPrice:N2}  المجموع: {item.TotalPrice:N2}");

                if (item.MenuItem != null && item.UnitPrice < item.MenuItem.Price)
                {
                    var disc = (item.MenuItem.Price - item.UnitPrice) * item.Quantity;
                    sb.AppendLine($"  خصم: {disc:N2} SDG");
                }
                sb.AppendLine();
            }

            sb.AppendLine(new string('-', 32));

            var totalDiscount = order.OrderItems
                .Where(i => i.MenuItem != null)
                .Sum(i => (i.MenuItem!.Price - i.UnitPrice) * i.Quantity);

            if (totalDiscount > 0)
            {
                sb.AppendLine($"قبل الخصم: {order.TotalAmount + totalDiscount:N2} SDG");
                sb.AppendLine($"الخصم: {totalDiscount:N2} SDG");
            }

            sb.AppendLine($"الإجمالي: {order.TotalAmount:N2} SDG");
            sb.AppendLine();
            sb.AppendLine("================");
            sb.AppendLine("شكراً لزيارتكم");
            sb.AppendLine("نتمنى لكم تجربة ممتعة");
            sb.AppendLine("================");

            return sb.ToString();
        }

        /// <summary>
        /// Saves a plain-text receipt to the local AppData folder.
        /// </summary>
        public async Task<string> SaveReceiptToFileAsync(string receiptText, string orderNumber)
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JamrahPOS", "Receipts");

            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, $"Receipt_{orderNumber}_{DateTime.Now:yyyyMMddHHmmss}.txt");
            await File.WriteAllTextAsync(path, receiptText, Encoding.UTF8);
            return path;
        }

        /// <summary>
        /// Opens a saved receipt file in the default text viewer.
        /// </summary>
        public void OpenReceipt(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = filePath,
                    UseShellExecute = true
                });
            }
            catch { /* ignore */ }
        }
    }
}
