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
            
            // Redirect console output to file
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "JamrahPOS",
                    "app.log"
                );
                
                _logFile = new StreamWriter(logPath, append: true)
                {
                    AutoFlush = true
                };
                
                Console.SetOut(_logFile);
                Console.WriteLine($"\n=== Application Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
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
    }
}
