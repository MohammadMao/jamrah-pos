using System.Windows;
using JamrahPOS.Services;

namespace JamrahPOS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize database on startup
            try
            {
                var databaseService = new DatabaseService();
                await databaseService.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize database: {ex.Message}", 
                    "Database Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
