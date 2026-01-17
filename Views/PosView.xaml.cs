using System.Windows.Controls;
using JamrahPOS.Models;
using JamrahPOS.ViewModels;

namespace JamrahPOS.Views
{
    /// <summary>
    /// Interaction logic for PosView.xaml
    /// </summary>
    public partial class PosView : UserControl
    {
        public PosView()
        {
            InitializeComponent();
        }

        private void CategoryButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Category category)
            {
                if (DataContext is PosViewModel viewModel)
                {
                    viewModel.SelectedCategory = category;
                }
            }
        }
    }
}
