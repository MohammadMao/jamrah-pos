using System.Globalization;
using JamrahPOS.Data;
using JamrahPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace JamrahPOS.Services
{
    /// <summary>
    /// Service for generating business reports
    /// </summary>
    public class ReportService
    {
        private readonly PosDbContext _context;

        public ReportService(PosDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets daily sales reports for a date range
        /// </summary>
        public async Task<List<DailySalesReport>> GetDailySalesReportsAsync(DateTime startDate, DateTime endDate)
        {
            var reports = new List<DailySalesReport>();

            // Normalize dates to start and end of day
            startDate = startDate.Date;
            endDate = endDate.Date.AddDays(1).AddSeconds(-1);

            // Get all orders in the date range (include OrderItems for menu item breakdown)
            var orders = await _context.Orders
                .Include(o => o.Cashier)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.MenuItem)
                .Where(o => !o.IsVoided && o.OrderDateTime >= startDate && o.OrderDateTime <= endDate)
                .OrderBy(o => o.OrderDateTime)
                .ToListAsync();

            var dailyReports = BuildDailySalesReports(orders);
            return dailyReports;
        }

        public async Task<List<Order>> GetDailySalesOrdersAsync(DateTime startDate, DateTime endDate)
        {
            startDate = startDate.Date;
            endDate = endDate.Date.AddDays(1).AddSeconds(-1);

            return await _context.Orders
                .Include(o => o.Cashier)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.MenuItem)
                .Where(o => !o.IsVoided && o.OrderDateTime >= startDate && o.OrderDateTime <= endDate)
                .OrderBy(o => o.OrderDateTime)
                .ToListAsync();
        }

        public List<DailySalesReport> BuildDailySalesReports(List<Order> orders)
        {
            var reports = new List<DailySalesReport>();
            var groupedByDate = orders.GroupBy(o => o.OrderDateTime.Date);

            foreach (var dateGroup in groupedByDate)
            {
                var dailyReport = new DailySalesReport
                {
                    Date = dateGroup.Key,
                    TotalSales = dateGroup.Sum(o => o.TotalAmount),
                    OrderCount = dateGroup.Count()
                };

                // Group by payment method
                var paymentMethods = dateGroup.GroupBy(o => o.PaymentMethod);
                foreach (var pmGroup in paymentMethods)
                {
                    var pmTotal = pmGroup.Sum(o => o.TotalAmount);
                    dailyReport.PaymentMethods.Add(new PaymentMethodSummary
                    {
                        PaymentMethod = pmGroup.Key,
                        TotalAmount = pmTotal,
                        OrderCount = pmGroup.Count(),
                        Percentage = dailyReport.TotalSales > 0 ? (pmTotal / dailyReport.TotalSales) * 100 : 0
                    });
                }

                // Group by cashier
                var cashiers = dateGroup.GroupBy(o => o.CashierId);
                foreach (var cashierGroup in cashiers)
                {
                    var cashierTotal = cashierGroup.Sum(o => o.TotalAmount);
                    var cashierName = cashierGroup.First().Cashier?.Username ?? "Unknown";
                    dailyReport.Cashiers.Add(new CashierSummary
                    {
                        CashierId = cashierGroup.Key,
                        CashierName = cashierName,
                        TotalAmount = cashierTotal,
                        OrderCount = cashierGroup.Count(),
                        Percentage = dailyReport.TotalSales > 0 ? (cashierTotal / dailyReport.TotalSales) * 100 : 0
                    });
                }

                // Aggregate per menu item
                var itemGroups = dateGroup
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(i => i.MenuItemId);

                foreach (var itemGroup in itemGroups)
                {
                    var first = itemGroup.First();
                    dailyReport.MenuItems.Add(new MenuItemSalesReport
                    {
                        MenuItemId   = itemGroup.Key,
                        Name         = first.MenuItem?.Name ?? "صنف",
                        TotalQuantity = itemGroup.Sum(i => i.Quantity),
                        UnitPrice    = first.UnitPrice,
                        TotalRevenue = itemGroup.Sum(i => i.TotalPrice)
                    });
                }

                // Sort items by revenue descending
                dailyReport.MenuItems = dailyReport.MenuItems
                    .OrderByDescending(m => m.TotalRevenue).ToList();

                reports.Add(dailyReport);
            }

            return reports;
        }

        /// <summary>
        /// Gets configured time options for report period selectors
        /// </summary>
        public async Task<List<TimeOption>> GetTimeOptionsAsync()
        {
            return await _context.TimeOptions
                .OrderBy(t => t.Hour)
                .ToListAsync();
        }

        /// <summary>
        /// Gets report period settings for display and filtering
        /// </summary>
        public async Task<List<ReportPeriodSetting>> GetReportPeriodSettingsAsync()
        {
            return await _context.ReportPeriodSettings
                .OrderBy(r => r.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Gets a single report period setting by ID
        /// </summary>
        public async Task<ReportPeriodSetting?> GetReportPeriodSettingAsync(int id)
        {
            return await _context.ReportPeriodSettings.FindAsync(id);
        }

        /// <summary>
        /// Saves period settings changes to the database
        /// </summary>
        public async Task SaveReportPeriodSettingsAsync(List<ReportPeriodSetting> periodSettings)
        {
            foreach (var setting in periodSettings)
            {
                var existing = await _context.ReportPeriodSettings.FindAsync(setting.Id);
                if (existing != null)
                {
                    existing.StartHour = setting.StartHour;
                    existing.EndHour = setting.EndHour;
                    existing.EndTimeMinutes = setting.EndTimeMinutes;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets weekly sales reports starting from Sunday
        /// </summary>
        public async Task<List<WeeklySalesReport>> GetWeeklySalesReportsAsync(DateTime startDate, DateTime endDate)
        {
            var reports = new List<WeeklySalesReport>();

            // Normalize start date to Sunday
            var current = startDate.Date;
            while (current.DayOfWeek != DayOfWeek.Sunday)
            {
                current = current.AddDays(-1);
            }

            // Normalize end date
            endDate = endDate.Date.AddDays(1).AddSeconds(-1);

            while (current < endDate)
            {
                var weekStart = current;
                var weekEnd = current.AddDays(7).AddSeconds(-1);

                // Get all orders for the week
                var orders = await _context.Orders
                    .Include(o => o.Cashier)
                    .Where(o => !o.IsVoided && o.OrderDateTime >= weekStart && o.OrderDateTime <= weekEnd)
                    .OrderBy(o => o.OrderDateTime)
                    .ToListAsync();

                if (orders.Count > 0)
                {
                    var weeklyReport = new WeeklySalesReport
                    {
                        WeekStartDate = weekStart,
                        WeekEndDate = weekEnd,
                        TotalSales = orders.Sum(o => o.TotalAmount),
                        OrderCount = orders.Count()
                    };

                    // Daily breakdown
                    var dailyGroups = orders.GroupBy(o => o.OrderDateTime.Date);
                    foreach (var dayGroup in dailyGroups)
                    {
                        var dailyReport = new DailySalesReport
                        {
                            Date = dayGroup.Key,
                            TotalSales = dayGroup.Sum(o => o.TotalAmount),
                            OrderCount = dayGroup.Count()
                        };
                        weeklyReport.DailyBreakdown.Add(dailyReport);
                    }

                    // Group by payment method
                    var paymentMethods = orders.GroupBy(o => o.PaymentMethod);
                    foreach (var pmGroup in paymentMethods)
                    {
                        var pmTotal = pmGroup.Sum(o => o.TotalAmount);
                        weeklyReport.PaymentMethods.Add(new PaymentMethodSummary
                        {
                            PaymentMethod = pmGroup.Key,
                            TotalAmount = pmTotal,
                            OrderCount = pmGroup.Count(),
                            Percentage = weeklyReport.TotalSales > 0 ? (pmTotal / weeklyReport.TotalSales) * 100 : 0
                        });
                    }

                    // Group by cashier
                    var cashiers = orders.GroupBy(o => o.CashierId);
                    foreach (var cashierGroup in cashiers)
                    {
                        var cashierTotal = cashierGroup.Sum(o => o.TotalAmount);
                        var cashierName = cashierGroup.First().Cashier?.Username ?? "Unknown";
                        weeklyReport.Cashiers.Add(new CashierSummary
                        {
                            CashierId = cashierGroup.Key,
                            CashierName = cashierName,
                            TotalAmount = cashierTotal,
                            OrderCount = cashierGroup.Count(),
                            Percentage = weeklyReport.TotalSales > 0 ? (cashierTotal / weeklyReport.TotalSales) * 100 : 0
                        });
                    }

                    reports.Add(weeklyReport);
                }

                current = current.AddDays(7);
            }

            return reports;
        }

        /// <summary>
        /// Gets monthly sales reports
        /// </summary>
        public async Task<List<MonthlySalesReport>> GetMonthlySalesReportsAsync(int year, int month)
        {
            var reports = new List<MonthlySalesReport>();

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddSeconds(-1);

            // Get all orders for the month
            var orders = await _context.Orders
                .Include(o => o.Cashier)
                .Where(o => !o.IsVoided && o.OrderDateTime >= startDate && o.OrderDateTime <= endDate)
                .OrderBy(o => o.OrderDateTime)
                .ToListAsync();

            var monthlyReport = new MonthlySalesReport
            {
                Year = year,
                Month = month,
                TotalSales = orders.Sum(o => o.TotalAmount),
                OrderCount = orders.Count()
            };

            // Daily breakdown
            var dailyGroups = orders.GroupBy(o => o.OrderDateTime.Date);
            foreach (var dayGroup in dailyGroups)
            {
                var dailyReport = new DailySalesReport
                {
                    Date = dayGroup.Key,
                    TotalSales = dayGroup.Sum(o => o.TotalAmount),
                    OrderCount = dayGroup.Count()
                };
                monthlyReport.DailyBreakdown.Add(dailyReport);
            }

            // Group by payment method
            var paymentMethods = orders.GroupBy(o => o.PaymentMethod);
            foreach (var pmGroup in paymentMethods)
            {
                var pmTotal = pmGroup.Sum(o => o.TotalAmount);
                monthlyReport.PaymentMethods.Add(new PaymentMethodSummary
                {
                    PaymentMethod = pmGroup.Key,
                    TotalAmount = pmTotal,
                    OrderCount = pmGroup.Count(),
                    Percentage = monthlyReport.TotalSales > 0 ? (pmTotal / monthlyReport.TotalSales) * 100 : 0
                });
            }

            // Group by cashier
            var cashiers = orders.GroupBy(o => o.CashierId);
            foreach (var cashierGroup in cashiers)
            {
                var cashierTotal = cashierGroup.Sum(o => o.TotalAmount);
                var cashierName = cashierGroup.First().Cashier?.Username ?? "Unknown";
                monthlyReport.Cashiers.Add(new CashierSummary
                {
                    CashierId = cashierGroup.Key,
                    CashierName = cashierName,
                    TotalAmount = cashierTotal,
                    OrderCount = cashierGroup.Count(),
                    Percentage = monthlyReport.TotalSales > 0 ? (cashierTotal / monthlyReport.TotalSales) * 100 : 0
                });
            }

            reports.Add(monthlyReport);
            return reports;
        }

        /// <summary>
        /// Gets monthly sales reports for a range of months
        /// </summary>
        public async Task<List<MonthlySalesReport>> GetMonthlyRangeReportsAsync(DateTime startDate, DateTime endDate)
        {
            var reports = new List<MonthlySalesReport>();

            var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
            endDate = new DateTime(endDate.Year, endDate.Month, 1);

            while (currentDate <= endDate)
            {
                var monthReports = await GetMonthlySalesReportsAsync(currentDate.Year, currentDate.Month);
                reports.AddRange(monthReports);
                currentDate = currentDate.AddMonths(1);
            }

            return reports;
        }
    }
}
