using System.Diagnostics;
using System.IO;
using System.Text;
using JamrahPOS.Models;

namespace JamrahPOS.Services
{
    /// <summary>
    /// Service for generating and printing receipts
    /// </summary>
    public class PrintService
    {
        private const string STORE_NAME = "مطعم جمرة";
        private const string STORE_SLOGAN = "أصل الطعم المشوي";
        private const string STORE_ADDRESS = "الخرطوم بحري";
        private const string STORE_PHONE = "0912147130";
        private const string RECEIPT_WIDTH = "========================================";

        /// <summary>
        /// Generates a receipt text for an order
        /// </summary>
        public string GenerateReceipt(Order order, User cashier)
        {
            var receipt = new StringBuilder();

            // Header
            receipt.AppendLine(RECEIPT_WIDTH);
            receipt.AppendLine(CenterText(STORE_NAME));
            receipt.AppendLine(CenterText(STORE_SLOGAN));
            receipt.AppendLine(CenterText(STORE_ADDRESS));
            receipt.AppendLine(CenterText($"هاتف: {STORE_PHONE}"));
            receipt.AppendLine(RECEIPT_WIDTH);
            receipt.AppendLine();

            // Order Information
            receipt.AppendLine($"رقم الطلب: {order.OrderNumber}");
            receipt.AppendLine($"التاريخ: {order.OrderDateTime:yyyy/MM/dd}");
            receipt.AppendLine($"الوقت: {order.OrderDateTime:HH:mm:ss}");
            receipt.AppendLine($"الكاشير: {cashier.Username}");
            receipt.AppendLine($"طريقة الدفع: {order.PaymentMethod}");
            receipt.AppendLine(RECEIPT_WIDTH);
            receipt.AppendLine();

            // Items Header
            receipt.AppendLine(FormatLine("الصنف", "الكمية", "السعر", "المجموع"));
            receipt.AppendLine(new string('-', 40));

            // Order Items
            foreach (var item in order.OrderItems)
            {
                var menuItem = item.MenuItem;
                var name = menuItem?.Name ?? "صنف";
                
                // Truncate long names
                if (name.Length > 15)
                    name = name.Substring(0, 12) + "...";

                receipt.AppendLine(FormatItemLine(
                    name,
                    item.Quantity.ToString(),
                    $"{item.UnitPrice:N2}",
                    $"{item.TotalPrice:N2}"
                ));

                // Show discount if applied
                if (menuItem != null && item.UnitPrice < menuItem.Price)
                {
                    var discount = (menuItem.Price - item.UnitPrice) * item.Quantity;
                    receipt.AppendLine($"  (خصم: {discount:N2} SDG)");
                }
            }

            receipt.AppendLine(new string('-', 40));
            receipt.AppendLine();

            // Totals
            var subtotal = order.OrderItems.Sum(i => i.TotalPrice);
            var totalDiscount = order.OrderItems
                .Where(i => i.MenuItem != null)
                .Sum(i => (i.MenuItem!.Price - i.UnitPrice) * i.Quantity);

            if (totalDiscount > 0)
            {
                var originalTotal = subtotal + totalDiscount;
                receipt.AppendLine(FormatTotalLine("المجموع الأصلي:", $"{originalTotal:N2} SDG"));
                receipt.AppendLine(FormatTotalLine("الخصم:", $"{totalDiscount:N2} SDG"));
            }

            receipt.AppendLine(FormatTotalLine("الإجمالي:", $"{order.TotalAmount:N2} SDG"));
            receipt.AppendLine();

            // Footer
            receipt.AppendLine(RECEIPT_WIDTH);
            receipt.AppendLine(CenterText("شكراً لزيارتكم"));
            receipt.AppendLine(CenterText("نتمنى لكم تجربة ممتعة"));
            receipt.AppendLine(RECEIPT_WIDTH);

            return receipt.ToString();
        }

        /// <summary>
        /// Prints a receipt to the default printer
        /// </summary>
        public async Task<bool> PrintReceiptAsync(string receiptText)
        {
            try
            {
                // Create temp file for printing
                var tempFile = Path.Combine(Path.GetTempPath(), $"receipt_{DateTime.Now:yyyyMMddHHmmss}.txt");
                await File.WriteAllTextAsync(tempFile, receiptText, Encoding.UTF8);

                // On Linux, try to print using lp command
                if (OperatingSystem.IsLinux())
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "lp",
                            Arguments = tempFile,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await process.WaitForExitAsync();

                    // Clean up temp file after a delay
                    _ = Task.Delay(5000).ContinueWith(_ => 
                    {
                        try { File.Delete(tempFile); } catch { }
                    });

                    return process.ExitCode == 0;
                }
                // On Windows, use notepad to print
                else if (OperatingSystem.IsWindows())
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "notepad.exe",
                            Arguments = $"/p {tempFile}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await process.WaitForExitAsync();

                    // Clean up temp file after a delay
                    _ = Task.Delay(5000).ContinueWith(_ => 
                    {
                        try { File.Delete(tempFile); } catch { }
                    });

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Saves receipt to a file
        /// </summary>
        public async Task<string> SaveReceiptToFileAsync(string receiptText, string orderNumber)
        {
            var receiptsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JamrahPOS",
                "Receipts"
            );

            Directory.CreateDirectory(receiptsDir);

            var fileName = $"Receipt_{orderNumber}_{DateTime.Now:yyyyMMddHHmmss}.txt";
            var filePath = Path.Combine(receiptsDir, fileName);

            await File.WriteAllTextAsync(filePath, receiptText, Encoding.UTF8);

            return filePath;
        }

        /// <summary>
        /// Opens the receipt in the default text editor
        /// </summary>
        public void OpenReceipt(string filePath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    }
                };
                process.Start();
            }
            catch (Exception)
            {
                // Ignore if can't open
            }
        }

        private string CenterText(string text)
        {
            var width = 40;
            if (text.Length >= width) return text;

            var padding = (width - text.Length) / 2;
            return new string(' ', padding) + text;
        }

        private string FormatLine(string col1, string col2, string col3, string col4)
        {
            return $"{col1,-15}{col2,5}{col3,10}{col4,10}";
        }

        private string FormatItemLine(string name, string qty, string price, string total)
        {
            return $"{name,-15}{qty,5}{price,10}{total,10}";
        }

        private string FormatTotalLine(string label, string amount)
        {
            return $"{label,25}{amount,15}";
        }
    }
}
