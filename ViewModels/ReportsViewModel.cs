using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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
        private ObservableCollection<MonthlySalesReport> _yearlyReports = new();
        private ObservableCollection<MonthlySalesReport> _monthlyReports = new();
        private List<DailySalesReport> _allDailyReports = new();
        
        private DateTime _startDate;
        private DateTime _endDate;
        private int _selectedReportType = 0; // 0: Daily, 1: Yearly, 2: Monthly, 3: Period settings
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        
        // Pagination for Daily Reports
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 7;
        
        // Year selection for Yearly Reports
        private int _selectedYear;
        private ObservableCollection<int> _availableYears = new();
        
        // Month selection for Monthly Reports
        private int _selectedMonth;
        private ObservableCollection<MonthOption> _availableMonths = new();

        // Period selection for Daily Reports
        private int _selectedDailyPeriod;
        private ObservableCollection<PeriodOption> _availableDailyPeriods = new();

        // Admin period settings
        private ObservableCollection<TimeOption> _availableHours = new();
        private int _selectedFirstPeriodStartHour;
        private int _selectedFirstPeriodEndHour;
        private int _selectedSecondPeriodStartHour;
        private int _selectedSecondPeriodEndHour;
        private bool _isAdmin;
        private ReportPeriodSetting? _firstPeriodSetting;
        private ReportPeriodSetting? _secondPeriodSetting;

        public ObservableCollection<DailySalesReport> DailyReports
        {
            get => _dailyReports;
            set => SetProperty(ref _dailyReports, value);
        }

        public ObservableCollection<MonthlySalesReport> YearlyReports
        {
            get => _yearlyReports;
            set => SetProperty(ref _yearlyReports, value);
        }

        public ObservableCollection<MonthlySalesReport> MonthlyReports
        {
            get => _monthlyReports;
            set => SetProperty(ref _monthlyReports, value);
        }
        
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdateDailyReportsPage();
                }
            }
        }
        
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }
        
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    _ = LoadReportsAsync();
                }
            }
        }
        
        public ObservableCollection<int> AvailableYears
        {
            get => _availableYears;
            set => SetProperty(ref _availableYears, value);
        }

        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (SetProperty(ref _selectedMonth, value))
                {
                    _ = LoadReportsAsync();
                }
            }
        }

        public ObservableCollection<MonthOption> AvailableMonths
        {
            get => _availableMonths;
            set => SetProperty(ref _availableMonths, value);
        }

        public int SelectedDailyPeriod
        {
            get => _selectedDailyPeriod;
            set
            {
                if (SetProperty(ref _selectedDailyPeriod, value))
                {
                    _ = LoadReportsAsync();
                }
            }
        }

        public ObservableCollection<PeriodOption> AvailableDailyPeriods
        {
            get => _availableDailyPeriods;
            set => SetProperty(ref _availableDailyPeriods, value);
        }

        public ObservableCollection<TimeOption> AvailableHours
        {
            get => _availableHours;
            set => SetProperty(ref _availableHours, value);
        }

        public int SelectedFirstPeriodStartHour
        {
            get => _selectedFirstPeriodStartHour;
            set => SetProperty(ref _selectedFirstPeriodStartHour, value);
        }

        public int SelectedFirstPeriodEndHour
        {
            get => _selectedFirstPeriodEndHour;
            set => SetProperty(ref _selectedFirstPeriodEndHour, value);
        }

        public int SelectedSecondPeriodStartHour
        {
            get => _selectedSecondPeriodStartHour;
            set => SetProperty(ref _selectedSecondPeriodStartHour, value);
        }

        public int SelectedSecondPeriodEndHour
        {
            get => _selectedSecondPeriodEndHour;
            set => SetProperty(ref _selectedSecondPeriodEndHour, value);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        public class MonthOption
        {
            public int Value { get; set; }
            public string Name { get; set; }
        }

        public class PeriodOption
        {
            public int Value { get; set; }
            public string Name { get; set; }
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
        public ICommand PrintCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand GoToPageCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand SavePeriodSettingsCommand { get; }

        public bool ShowReportFilters { get; }

        public ReportsViewModel(int initialReportType = 0, bool showReportFilters = true)
        {
            try
            {
                Console.WriteLine("[REPORTS] ReportsViewModel() constructor started");
                _startDate = DateTime.Now.Date;
                _endDate = DateTime.Now.Date;
                _selectedReportType = initialReportType;
                ShowReportFilters = showReportFilters;
                _selectedYear = DateTime.Now.Year;
                _selectedMonth = DateTime.Now.Month;
                _selectedDailyPeriod = 0;
                
                // Initialize available years (2026 onwards)
                InitializeAvailableYears();
                // Initialize available months
                InitializeAvailableMonths();
                // Initialize available daily periods
                InitializeAvailableDailyPeriods();

                // Initialize context and service - use default PosDbContext without custom options
                var context = new PosDbContext();
                _reportService = new ReportService(context);

                IsAdmin = SessionService.Instance.IsAdmin;
                RefreshCommand = new RelayCommand(_ => { _ = LoadReportsAsync(); });
                ExportCommand = new RelayCommand(_ => ExportToCSV());
                PrintCommand = new RelayCommand(_ => { _ = PrintReportsAsync(); });
                NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; });
                PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; });
                GoToPageCommand = new RelayCommand(param => { if (param is int page) CurrentPage = page; });
                ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
                var savePeriodSettingsCommand = new RelayCommand(_ => { _ = SavePeriodSettingsAsync(); });
                SavePeriodSettingsCommand = savePeriodSettingsCommand;
                Console.WriteLine("[REPORTS] ReportsViewModel initialized successfully");
                
                // Load initial reports and settings
                _ = LoadPeriodSettingsAsync();
                _ = LoadReportsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] CRITICAL ERROR in constructor: {ex.Message}");
                Console.WriteLine($"[REPORTS] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[REPORTS] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        public ReportsViewModel(PosDbContext context, int initialReportType = 0, bool showReportFilters = true)
        {
            try
            {
                Console.WriteLine("[REPORTS] ReportsViewModel(context) constructor started");
                _startDate = DateTime.Now.Date;
                _endDate = DateTime.Now.Date;
                _selectedReportType = initialReportType;
                ShowReportFilters = showReportFilters;
                _selectedYear = DateTime.Now.Year;
                _selectedMonth = DateTime.Now.Month;
                _selectedDailyPeriod = 0;
                
                // Initialize available years (2026 onwards)
                InitializeAvailableYears();
                // Initialize available months
                InitializeAvailableMonths();
                // Initialize available daily periods
                InitializeAvailableDailyPeriods();
                
                _reportService = new ReportService(context);

                IsAdmin = SessionService.Instance.IsAdmin;
                RefreshCommand = new RelayCommand(_ => { _ = LoadReportsAsync(); });
                ExportCommand = new RelayCommand(_ => ExportToCSV());
                PrintCommand = new RelayCommand(_ => { _ = PrintReportsAsync(); });
                NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; });
                PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; });
                GoToPageCommand = new RelayCommand(param => { if (param is int page) CurrentPage = page; });
                ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
                SavePeriodSettingsCommand = new RelayCommand(_ => { _ = SavePeriodSettingsAsync(); });
                Console.WriteLine("[REPORTS] ReportsViewModel initialized successfully");
                
                // Load initial reports and settings
                _ = LoadPeriodSettingsAsync();
                _ = LoadReportsAsync();
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
                    case 1: // Yearly Reports
                        await LoadYearlyReportsAsync();
                        break;
                    case 2: // Monthly Reports
                        await LoadSingleMonthReportAsync();
                        break;
                    case 3: // Period settings
                        StatusMessage = "يمكنك تعديل إعدادات الفترات";
                        return;
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
                var orders = await _reportService.GetDailySalesOrdersAsync(StartDate, EndDate);
                if (SelectedDailyPeriod == 1 && _firstPeriodSetting != null)
                {
                    var firstStart = TimeSpan.FromHours(_firstPeriodSetting.StartHour);
                    var firstEnd = TimeSpan.FromMinutes(_firstPeriodSetting.EndTimeMinutes);
                    orders = orders.Where(o => IsTimeInPeriod(o.OrderDateTime.TimeOfDay, firstStart, firstEnd)).ToList();
                }
                else if (SelectedDailyPeriod == 2 && _secondPeriodSetting != null)
                {
                    var secondStart = TimeSpan.FromHours(_secondPeriodSetting.StartHour);
                    var secondEnd = TimeSpan.FromMinutes(_secondPeriodSetting.EndTimeMinutes);
                    orders = orders.Where(o => IsTimeInPeriod(o.OrderDateTime.TimeOfDay, secondStart, secondEnd)).ToList();
                }

                var reports = _reportService.BuildDailySalesReports(orders);
                Console.WriteLine($"[REPORTS] Retrieved {reports.Count} daily reports");
                // Sort by date descending (newest first)
                _allDailyReports = reports.OrderByDescending(r => r.Date).ToList();
                
                // Calculate pagination
                TotalPages = (int)Math.Ceiling((double)_allDailyReports.Count / PageSize);
                CurrentPage = 1; // Reset to first page
                
                UpdateDailyReportsPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in LoadDailyReportsAsync: {ex.Message}");
                throw;
            }
        }

        private async Task LoadPeriodSettingsAsync()
        {
            try
            {
                var hours = await _reportService.GetTimeOptionsAsync();
                AvailableHours = new ObservableCollection<TimeOption>(hours);

                var periods = await _reportService.GetReportPeriodSettingsAsync();
                _firstPeriodSetting = periods.FirstOrDefault(p => p.Name == "الفترة الأولى" || p.Id == 1);
                _secondPeriodSetting = periods.FirstOrDefault(p => p.Name == "الفترة الثانية" || p.Id == 2);

                if (_firstPeriodSetting != null)
                {
                    SelectedFirstPeriodStartHour = _firstPeriodSetting.StartHour;
                    SelectedFirstPeriodEndHour = _firstPeriodSetting.EndHour;
                }

                if (_secondPeriodSetting != null)
                {
                    SelectedSecondPeriodStartHour = _secondPeriodSetting.StartHour;
                    SelectedSecondPeriodEndHour = _secondPeriodSetting.EndHour;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in LoadPeriodSettingsAsync: {ex.Message}");
                StatusMessage = "خطأ في تحميل إعدادات الفترات";
            }
        }

        private async Task SavePeriodSettingsAsync()
        {
            try
            {
                if (!IsAdmin)
                {
                    MessageBox.Show("هذه الميزة متاحة للمدير فقط.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_firstPeriodSetting == null || _secondPeriodSetting == null)
                {
                    MessageBox.Show("تعذر تحميل إعدادات الفترات الحالية.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _firstPeriodSetting.StartHour = SelectedFirstPeriodStartHour;
                _firstPeriodSetting.EndHour = SelectedFirstPeriodEndHour;
                _firstPeriodSetting.EndTimeMinutes = SelectedFirstPeriodEndHour == 0 ? 23 * 60 + 59 : SelectedFirstPeriodEndHour * 60;

                _secondPeriodSetting.StartHour = SelectedSecondPeriodStartHour;
                _secondPeriodSetting.EndHour = SelectedSecondPeriodEndHour;
                _secondPeriodSetting.EndTimeMinutes = SelectedSecondPeriodEndHour == 0 ? 23 * 60 + 59 : SelectedSecondPeriodEndHour * 60;

                await _reportService.SaveReportPeriodSettingsAsync(new List<ReportPeriodSetting> { _firstPeriodSetting, _secondPeriodSetting });
                StatusMessage = "تم حفظ إعدادات الفترات بنجاح";
                MessageBox.Show("تم حفظ إعدادات الفترات بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload filters and refresh the report data in case the user is viewing daily reports.
                _ = LoadReportsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in SavePeriodSettingsAsync: {ex.Message}");
                StatusMessage = "خطأ في حفظ إعدادات الفترات";
                MessageBox.Show($"حدث خطأ أثناء حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsTimeInPeriod(TimeSpan orderTime, TimeSpan start, TimeSpan end)
        {
            if (start <= end)
            {
                return orderTime >= start && orderTime <= end;
            }

            return orderTime >= start || orderTime <= end;
        }

        private void UpdateDailyReportsPage()
        {
            var skip = (CurrentPage - 1) * PageSize;
            var pagedReports = _allDailyReports.Skip(skip).Take(PageSize);
            DailyReports = new ObservableCollection<DailySalesReport>(pagedReports);
        }

        /// <summary>
        /// Loads yearly sales reports (all months of a year)
        /// </summary>
        private async Task LoadYearlyReportsAsync()
        {
            try
            {
                Console.WriteLine($"[REPORTS] Loading yearly reports for year {SelectedYear}");
                
                // Determine start and end dates based on selected year
                DateTime startDate, endDate;
                
                if (SelectedYear == DateTime.Now.Year)
                {
                    // Current year: from January to current month
                    startDate = new DateTime(SelectedYear, 1, 1);
                    endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
                }
                else
                {
                    // Past years: full year (January to December)
                    startDate = new DateTime(SelectedYear, 1, 1);
                    endDate = new DateTime(SelectedYear, 12, 31);
                }
                
                var reports = await _reportService.GetMonthlyRangeReportsAsync(startDate, endDate);
                Console.WriteLine($"[REPORTS] Retrieved {reports.Count} yearly reports");
                // Sort by date descending (newest first)
                YearlyReports = new ObservableCollection<MonthlySalesReport>(reports.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in LoadYearlyReportsAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads a single month sales report
        /// </summary>
        private async Task LoadSingleMonthReportAsync()
        {
            try
            {
                Console.WriteLine($"[REPORTS] Loading monthly report for {SelectedMonth}/{SelectedYear}");
                
                var reports = await _reportService.GetMonthlySalesReportsAsync(SelectedYear, SelectedMonth);
                Console.WriteLine($"[REPORTS] Retrieved {reports.Count} monthly report");
                MonthlyReports = new ObservableCollection<MonthlySalesReport>(reports);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORTS] ERROR in LoadSingleMonthReportAsync: {ex.Message}");
                throw;
            }
        }
        
        private void InitializeAvailableYears()
        {
            // Start from 2026 up to current year
            var years = new List<int>();
            for (int year = 2026; year <= DateTime.Now.Year; year++)
            {
                years.Add(year);
            }
            AvailableYears = new ObservableCollection<int>(years.OrderByDescending(y => y));
        }

        private void InitializeAvailableMonths()
        {
            var months = new List<MonthOption>
            {
                new MonthOption { Value = 1, Name = "يناير" },
                new MonthOption { Value = 2, Name = "فبراير" },
                new MonthOption { Value = 3, Name = "مارس" },
                new MonthOption { Value = 4, Name = "أبريل" },
                new MonthOption { Value = 5, Name = "مايو" },
                new MonthOption { Value = 6, Name = "يونيو" },
                new MonthOption { Value = 7, Name = "يوليو" },
                new MonthOption { Value = 8, Name = "أغسطس" },
                new MonthOption { Value = 9, Name = "سبتمبر" },
                new MonthOption { Value = 10, Name = "أكتوبر" },
                new MonthOption { Value = 11, Name = "نوفمبر" },
                new MonthOption { Value = 12, Name = "ديسمبر" }
            };
            AvailableMonths = new ObservableCollection<MonthOption>(months);
        }

        private void InitializeAvailableDailyPeriods()
        {
            var periods = new List<PeriodOption>
            {
                new PeriodOption { Value = 0, Name = "الكل" },
                new PeriodOption { Value = 1, Name = "الفترة الأولى" },
                new PeriodOption { Value = 2, Name = "الفترة الثانية" }
            };
            AvailableDailyPeriods = new ObservableCollection<PeriodOption>(periods);
        }
        
        private void ResetFilters()
        {
            StartDate = DateTime.Now.Date;
            EndDate = DateTime.Now.Date;
            SelectedDailyPeriod = 0;
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
                        case 1: // Yearly
                            ExportYearlyReportsToCSV(writer);
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
        /// Exports yearly reports to CSV
        /// </summary>
        private void ExportYearlyReportsToCSV(StreamWriter writer)
        {
            writer.WriteLine("Yearly Sales Report");
            writer.WriteLine();

            foreach (var report in YearlyReports)
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

        /// <summary>
        /// Prints the current reports
        /// </summary>
        private async Task PrintReportsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري تحضير التقرير للطباعة...";

                string reportText = "";

                switch (SelectedReportType)
                {
                    case 0: // Daily
                        reportText = GenerateDailyReportsText();
                        break;
                    case 1: // Yearly
                        reportText = GenerateYearlyReportsText();
                        break;
                    case 2: // Monthly
                        reportText = GenerateMonthlyReportsText();
                        break;
                }

                var printService = new PrintService();
                bool result = await printService.PrintReportAsync(reportText);

                if (result)
                {
                    StatusMessage = "تم إرسال التقرير للطباعة بنجاح";
                    MessageBox.Show("تم إرسال التقرير للطباعة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "فشل في إرسال التقرير للطباعة";
                    MessageBox.Show("فشل في إرسال التقرير للطباعة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في الطباعة: {ex.Message}";
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Generates formatted text for daily reports
        /// </summary>
        private string GenerateDailyReportsText()
        {
            var sb = new StringBuilder();

            sb.AppendLine("================");
            sb.AppendLine(CenterText("تقرير يومي"));
            sb.AppendLine("================");
            sb.AppendLine($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd   HH:mm:ss}");
            sb.AppendLine();

            if (DailyReports.Count == 0)
            {
                sb.AppendLine("لا توجد بيانات");
                return sb.ToString();
            }

            foreach (var report in DailyReports)
            {
                sb.AppendLine($"التاريخ: {report.FormattedDate}");
                sb.AppendLine($"المبيعات: {report.TotalSales:F2} SDG");
                sb.AppendLine($"الطلبات: {report.OrderCount}");
                sb.AppendLine();

                // Menu Items Breakdown
                if (report.MenuItems.Count > 0)
                {
                    sb.AppendLine("تفصيل المبيعات:");
                    sb.AppendLine(new string('-', 32));
                    foreach (var item in report.MenuItems)
                    {
                        sb.AppendLine($"• {item.Name}");
                        sb.AppendLine($"  الكمية: {item.TotalQuantity}   السعر: {item.UnitPrice:F2}   الإجمالي: {item.TotalRevenue:F2}");
                    }
                    sb.AppendLine();
                }

                // Payment Methods - Simple list
                if (report.PaymentMethods.Count > 0)
                {
                    sb.AppendLine("طرق الدفع:");
                    sb.AppendLine(new string('-', 32));
                    foreach (var pm in report.PaymentMethods)
                    {
                        sb.AppendLine($"• {pm.PaymentMethod}");
                        sb.AppendLine($"  المبلغ: {pm.TotalAmount:F2}");
                        sb.AppendLine($"  العدد: {pm.OrderCount}");
                    }
                    sb.AppendLine();
                }

                // Cashiers - Simple list
                if (report.Cashiers.Count > 0)
                {
                    sb.AppendLine("الكاشيرون:");
                    sb.AppendLine(new string('-', 32));
                    foreach (var cashier in report.Cashiers)
                    {
                        sb.AppendLine($"• {cashier.CashierName}");
                        sb.AppendLine($"  المبلغ: {cashier.TotalAmount:F2}");
                        sb.AppendLine($"  العدد: {cashier.OrderCount}");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine(new string('=', 32));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates formatted text for yearly reports
        /// </summary>
        private string GenerateYearlyReportsText()
        {
            var sb = new StringBuilder();

            sb.AppendLine("================");
            sb.AppendLine(CenterText("تقرير سنوي"));
            sb.AppendLine("================");
            sb.AppendLine($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd   HH:mm:ss}");
            sb.AppendLine();

            if (YearlyReports.Count == 0)
            {
                sb.AppendLine("لا توجد بيانات");
                return sb.ToString();
            }

            foreach (var report in YearlyReports)
            {
                sb.AppendLine($"الشهر: {report.FormattedPeriod}");
                sb.AppendLine($"المبيعات: {report.TotalSales:F2} SDG");
                sb.AppendLine($"الطلبات: {report.OrderCount}");
                sb.AppendLine();

                // Payment Methods - Simple list
                if (report.PaymentMethods.Count > 0)
                {
                    sb.AppendLine("طرق الدفع:");
                    sb.AppendLine(new string('-', 16));
                    foreach (var pm in report.PaymentMethods)
                    {
                        sb.AppendLine($"• {pm.PaymentMethod}");
                        sb.AppendLine($"  المبلغ: {pm.TotalAmount:F2}");
                        sb.AppendLine($"  العدد: {pm.OrderCount}");
                    }
                    sb.AppendLine();
                }

                // Cashiers - Simple list
                if (report.Cashiers.Count > 0)
                {
                    sb.AppendLine("الكاشيرون:");
                    sb.AppendLine(new string('-', 16));
                    foreach (var cashier in report.Cashiers)
                    {
                        sb.AppendLine($"• {cashier.CashierName}");
                        sb.AppendLine($"  المبلغ: {cashier.TotalAmount:F2}");
                        sb.AppendLine($"  العدد: {cashier.OrderCount}");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine(new string('=', 16));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates formatted text for monthly reports
        /// </summary>
        private string GenerateMonthlyReportsText()
        {
            var sb = new StringBuilder();

            sb.AppendLine("================");
            sb.AppendLine(CenterText("تقرير شهري"));
            sb.AppendLine("================");
            sb.AppendLine($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd   HH:mm:ss}");
            sb.AppendLine();

            if (MonthlyReports.Count == 0)
            {
                sb.AppendLine("لا توجد بيانات");
                return sb.ToString();
            }

            foreach (var report in MonthlyReports)
            {
                sb.AppendLine($"الشهر: {report.FormattedPeriod}");
                sb.AppendLine($"المبيعات: {report.TotalSales:F2} SDG");
                sb.AppendLine($"الطلبات: {report.OrderCount}");
                sb.AppendLine();

                // Payment Methods - Simple list
                if (report.PaymentMethods.Count > 0)
                {
                    sb.AppendLine("طرق الدفع:");
                    sb.AppendLine(new string('-', 16));
                    foreach (var pm in report.PaymentMethods)
                    {
                        sb.AppendLine($"• {pm.PaymentMethod}");
                        sb.AppendLine($"  المبلغ: {pm.TotalAmount:F2}");
                        sb.AppendLine($"  العدد: {pm.OrderCount}");
                    }
                    sb.AppendLine();
                }

                // Cashiers - Simple list
                if (report.Cashiers.Count > 0)
                {
                    sb.AppendLine("الكاشيرون:");
                    sb.AppendLine(new string('-', 16));
                    foreach (var cashier in report.Cashiers)
                    {
                        sb.AppendLine($"• {cashier.CashierName}");
                        sb.AppendLine($"  المبلغ: {cashier.TotalAmount:F2}");
                        sb.AppendLine($"  العدد: {cashier.OrderCount}");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine(new string('=', 16));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Centers text for display (minimal padding for thermal printers)
        /// </summary>
        private string CenterText(string text)
        {
            const int width = 16;
            if (text.Length >= width) return text;

            var padding = (width - text.Length) / 2;
            return new string(' ', Math.Max(0, padding / 2)) + text;
        }
    }
}
