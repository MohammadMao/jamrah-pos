using System.Windows;
using JamrahPOS.Services;
using System.IO;

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
    }
}
