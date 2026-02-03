using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using JamrahPOS.Data;
using JamrahPOS.Helpers;
using JamrahPOS.Models;
using JamrahPOS.Services;
using Microsoft.EntityFrameworkCore;

namespace JamrahPOS.ViewModels
{
    /// <summary>
    /// ViewModel for the Reports screen
    /// </summary>
    public class ReportsViewModel : BaseViewModel
    {
        private readonly ReportService _reportService;
        private ObservableCollection<DailySalesReport> _dailyReports = new();
        private ObservableCollection<WeeklySalesReport> _weeklyReports = new();
        private ObservableCollection<MonthlySalesReport> _monthlyReports = new();
        
        private DateTime _startDate;
        private DateTime _endDate;
        private int _selectedReportType = 0; // 0: Daily, 1: Weekly, 2: Monthly
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        public ObservableCollection<DailySalesReport> DailyReports
        {
            get => _dailyReports;
            set => SetProperty(ref _dailyReports, value);
        }

        public ObservableCollection<WeeklySalesReport> WeeklyReports
        {
            get => _weeklyReports;
            set => SetProperty(ref _weeklyReports, value);
        }

        public ObservableCollection<MonthlySalesReport> MonthlyReports
        {
            get => _monthlyReports;
            set => SetProperty(ref _monthlyReports, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    // Auto-load when dates change
                    _ = LoadReportsAsync();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    // Auto-load when dates change
                    _ = LoadReportsAsync();
                }
            }
        }

        public int SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                if (SetProperty(ref _selectedReportType, value))
                {
                    _ = LoadReportsAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }

        public ReportsViewModel()
        {
            try
            {
                Console.WriteLine("[REPORTS] ReportsViewModel() constructor started");
                _startDate = DateTime.Now.AddDays(-30);
                _endDate = DateTime.Now;

                // Initialize context and service - use default PosDbContext without custom options
                var context = new PosDbContext();
                _reportService = new ReportService(context);

                RefreshCommand = new RelayCommand(_ => { _ = LoadReportsAsync(); });
                ExportCommand = new RelayCommand(_ => ExportToCSV());
                Console.WriteLine("[REPORTS] ReportsViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] CRITICAL ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[REPORTS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[REPORTS] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        public ReportsViewModel(PosDbContext context)
        {
            try
            {
                Console.WriteLine("[REPORTS] ReportsViewModel(context) constructor started");
                _startDate = DateTime.Now.AddDays(-30);
                _endDate = DateTime.Now;
                _reportService = new ReportService(context);

                RefreshCommand = new RelayCommand(_ => { _ = LoadReportsAsync(); });
                ExportCommand = new RelayCommand(_ => ExportToCSV());
                Console.WriteLine("[REPORTS] ReportsViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] CRITICAL ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[REPORTS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[REPORTS] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads the appropriate reports based on selected type
        /// </summary>
        private async Task LoadReportsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري تحميل التقارير...";

                switch (SelectedReportType)
                {
                    case 0: // Daily Reports
                        await LoadDailyReportsAsync();
                        break;
                    case 1: // Weekly Reports
                        await LoadWeeklyReportsAsync();
                        break;
                    case 2: // Monthly Reports
                        await LoadMonthlyReportsAsync();
                        break;
                }

                StatusMessage = "تم تحميل التقارير بنجاح";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in LoadReportsAsync: {ex.Message}");
                Console.WriteLine($"[REPORTS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[REPORTS] Inner exception: {ex.InnerException?.Message}");
                StatusMessage = $"خطأ في تحميل التقارير: {ex.Message}";
                MessageBox.Show($"Error loading reports: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Loads daily sales reports
        /// </summary>
        private async Task LoadDailyReportsAsync()
        {
            try
            {
                Console.WriteLine($"[REPORTS] Loading daily reports from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
                var reports = await _reportService.GetDailySalesReportsAsync(StartDate, EndDate);
                Console.WriteLine($"[REPORTS] Retrieved {reports.Count} daily reports");
                DailyReports = new ObservableCollection<DailySalesReport>(reports);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in LoadDailyReportsAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads weekly sales reports
        /// </summary>
        private async Task LoadWeeklyReportsAsync()
        {
            try
            {
                Console.WriteLine($"[REPORTS] Loading weekly reports from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
                var reports = await _reportService.GetWeeklySalesReportsAsync(StartDate, EndDate);
                Console.WriteLine($"[REPORTS] Retrieved {reports.Count} weekly reports");
                WeeklyReports = new ObservableCollection<WeeklySalesReport>(reports);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in LoadWeeklyReportsAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads monthly sales reports
        /// </summary>
        private async Task LoadMonthlyReportsAsync()
        {
            try
            {
                Console.WriteLine($"[REPORTS] Loading monthly reports from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
                var reports = await _reportService.GetMonthlyRangeReportsAsync(StartDate, EndDate);
                Console.WriteLine($"[REPORTS] Retrieved {reports.Count} monthly reports");
                MonthlyReports = new ObservableCollection<MonthlySalesReport>(reports);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in LoadMonthlyReportsAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Exports current reports to CSV
        /// </summary>
        private void ExportToCSV()
        {
            try
            {
                // Save to Documents folder
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string filePath = Path.Combine(documentsPath, fileName);

                using (var writer = new StreamWriter(filePath))
                {
                    switch (SelectedReportType)
                    {
                        case 0: // Daily
                            ExportDailyReportsToCSV(writer);
                            break;
                        case 1: // Weekly
                            ExportWeeklyReportsToCSV(writer);
                            break;
                        case 2: // Monthly
                            ExportMonthlyReportsToCSV(writer);
                            break;
                    }
                }

                MessageBox.Show($"تم تصدير التقرير بنجاح إلى:\n{filePath}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exports daily reports to CSV
        /// </summary>
        private void ExportDailyReportsToCSV(StreamWriter writer)
        {
            writer.WriteLine("Daily Sales Report");
            writer.WriteLine($"Period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
            writer.WriteLine();

            foreach (var report in DailyReports)
            {
                writer.WriteLine($"Date,{report.FormattedDate}");
                writer.WriteLine($"Total Sales,{report.TotalSales:F2}");
                writer.WriteLine($"Order Count,{report.OrderCount}");
                writer.WriteLine($"Average Order Value,{report.AverageOrderValue:F2}");
                writer.WriteLine();

                writer.WriteLine("Payment Methods");
                writer.WriteLine("Method,Total Amount,Order Count,Percentage");
                foreach (var pm in report.PaymentMethods)
                {
                    writer.WriteLine($"{pm.PaymentMethod},{pm.TotalAmount:F2},{pm.OrderCount},{pm.Percentage:F2}%");
                }
                writer.WriteLine();

                writer.WriteLine("Cashiers");
                writer.WriteLine("Name,Total Amount,Order Count,Percentage");
                foreach (var cashier in report.Cashiers)
                {
                    writer.WriteLine($"{cashier.CashierName},{cashier.TotalAmount:F2},{cashier.OrderCount},{cashier.Percentage:F2}%");
                }
                writer.WriteLine();
                writer.WriteLine("---");
                writer.WriteLine();
            }
        }

        /// <summary>
        /// Exports weekly reports to CSV
        /// </summary>
        private void ExportWeeklyReportsToCSV(StreamWriter writer)
        {
            writer.WriteLine("Weekly Sales Report");
            writer.WriteLine($"Period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
            writer.WriteLine();

            foreach (var report in WeeklyReports)
            {
                writer.WriteLine($"Week,{report.FormattedPeriod}");
                writer.WriteLine($"Total Sales,{report.TotalSales:F2}");
                writer.WriteLine($"Order Count,{report.OrderCount}");
                writer.WriteLine($"Average Order Value,{report.AverageOrderValue:F2}");
                writer.WriteLine();

                writer.WriteLine("Payment Methods");
                writer.WriteLine("Method,Total Amount,Order Count,Percentage");
                foreach (var pm in report.PaymentMethods)
                {
                    writer.WriteLine($"{pm.PaymentMethod},{pm.TotalAmount:F2},{pm.OrderCount},{pm.Percentage:F2}%");
                }
                writer.WriteLine();

                writer.WriteLine("Cashiers");
                writer.WriteLine("Name,Total Amount,Order Count,Percentage");
                foreach (var cashier in report.Cashiers)
                {
                    writer.WriteLine($"{cashier.CashierName},{cashier.TotalAmount:F2},{cashier.OrderCount},{cashier.Percentage:F2}%");
                }
                writer.WriteLine();
                writer.WriteLine("---");
                writer.WriteLine();
            }
        }

        /// <summary>
        /// Exports monthly reports to CSV
        /// </summary>
        private void ExportMonthlyReportsToCSV(StreamWriter writer)
        {
            writer.WriteLine("Monthly Sales Report");
            writer.WriteLine();

            foreach (var report in MonthlyReports)
            {
                writer.WriteLine($"Month,{report.FormattedPeriod}");
                writer.WriteLine($"Total Sales,{report.TotalSales:F2}");
                writer.WriteLine($"Order Count,{report.OrderCount}");
                writer.WriteLine($"Average Order Value,{report.AverageOrderValue:F2}");
                writer.WriteLine();

                writer.WriteLine("Payment Methods");
                writer.WriteLine("Method,Total Amount,Order Count,Percentage");
                foreach (var pm in report.PaymentMethods)
                {
                    writer.WriteLine($"{pm.PaymentMethod},{pm.TotalAmount:F2},{pm.OrderCount},{pm.Percentage:F2}%");
                }
                writer.WriteLine();

                writer.WriteLine("Cashiers");
                writer.WriteLine("Name,Total Amount,Order Count,Percentage");
                foreach (var cashier in report.Cashiers)
                {
                    writer.WriteLine($"{cashier.CashierName},{cashier.TotalAmount:F2},{cashier.OrderCount},{cashier.Percentage:F2}%");
                }
                writer.WriteLine();
                writer.WriteLine("---");
                writer.WriteLine();
            }
        }
    }
}
