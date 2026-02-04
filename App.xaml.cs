using System.Windows;
using JamrahPOS.Services;
using System.IO;
using System.Globalization;
using System.Threading;

namespace JamrahPOS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static StreamWriter? _logFile;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Set Arabic culture with SDG currency
            var culture = new CultureInfo("ar-SD"); // Arabic (Sudan)
            culture.NumberFormat.CurrencySymbol = "SDG";
            culture.NumberFormat.CurrencyDecimalDigits = 2;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            // Add global exception handler
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            // Redirect console output to file with rotation
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "JamrahPOS"
                );
                
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "app.log");
                
                // Rotate old log if exists
                RotateLogFile(logPath, logDir);
                
                _logFile = new StreamWriter(logPath, append: false) // Start fresh
                {
                    AutoFlush = true
                };
                
                Console.SetOut(_logFile);
                Console.WriteLine($"=== Application Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
            catch
            {
                // If log file fails, continue anyway
            }
            
            // Initialize database on startup
            try
            {
                Console.WriteLine("[APP] Starting database initialization...");
                var databaseService = new DatabaseService();
                await databaseService.InitializeDatabaseAsync();
                Console.WriteLine("[APP] Database initialization complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[APP] FAILED to initialize database: {ex.Message}");
                Console.WriteLine($"[APP] Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Failed to initialize database: {ex.Message}", 
                    "Database Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine($"[APP] Application shutting down: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logFile?.Flush();
            _logFile?.Dispose();
            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Console.WriteLine($"[FATAL] Unhandled exception: {ex?.Message}");
            Console.WriteLine($"[FATAL] Stack trace: {ex?.StackTrace}");
            Console.WriteLine($"[FATAL] Inner exception: {ex?.InnerException?.Message}");
            _logFile?.Flush();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"[ERROR] Dispatcher exception: {e.Exception.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {e.Exception.StackTrace}");
            Console.WriteLine($"[ERROR] Inner exception: {e.Exception.InnerException?.Message}");
            _logFile?.Flush();
            
            MessageBox.Show($"خطأ في التطبيق:\n{e.Exception.Message}\n\nتحقق من السجلات للمزيد من التفاصيل.", 
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            
            e.Handled = true;
        }

        /// <summary>
        /// Rotates log file by renaming old log with timestamp and cleaning up old logs
        /// </summary>
        private static void RotateLogFile(string currentLogPath, string logDir)
        {
            try
            {
                // If current log exists and has content, rotate it
                if (File.Exists(currentLogPath))
                {
                    var fileInfo = new FileInfo(currentLogPath);
                    if (fileInfo.Length > 0)
                    {
                        // Create rotated log filename with timestamp
                        string timestamp = File.GetLastWriteTime(currentLogPath).ToString("yyyy-MM-dd_HH-mm-ss");
                        string rotatedLogPath = Path.Combine(logDir, $"app_{timestamp}.log");
                        
                        // Rename current log to rotated name
                        File.Move(currentLogPath, rotatedLogPath);
                    }
                    else
                    {
                        // Empty log, just delete it
                        File.Delete(currentLogPath);
                    }
                }
                
                // Clean up old log files (keep last 10, delete older than 7 days)
                CleanupOldLogs(logDir);
            }
            catch
            {
                // If rotation fails, continue anyway
            }
        }

        /// <summary>
        /// Deletes old log files keeping only last 10 and removing files older than 7 days
        /// </summary>
        private static void CleanupOldLogs(string logDir)
        {
            try
            {
                var logFiles = Directory.GetFiles(logDir, "app_*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                // Keep last 10 log files, delete the rest
                var filesToDelete = logFiles.Skip(10).ToList();
                
                // Also delete any logs older than 7 days
                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                filesToDelete.AddRange(logFiles.Where(f => f.LastWriteTime < sevenDaysAgo));
                
                foreach (var file in filesToDelete.Distinct())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Ignore individual file deletion errors
                    }
                }
            }
            catch
            {
                // If cleanup fails, continue anyway
            }
        }
    }
}
