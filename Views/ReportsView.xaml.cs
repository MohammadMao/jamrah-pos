using System.Windows.Controls;
using JamrahPOS.ViewModels;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for ReportsView.xaml
    /// </summary>
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            DataContext = new ReportsViewModel();
        }

        public ReportsView(int initialReportType, bool showReportFilters = true)
        {
            InitializeComponent();
            DataContext = new ReportsViewModel(initialReportType, showReportFilters);
        }
    }
}
