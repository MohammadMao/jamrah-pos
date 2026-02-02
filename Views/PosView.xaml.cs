using System.Windows.Controls;
using System.Windows.Media;
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
            
            // Subscribe to DataContext changes to handle category selection highlighting
            DataContextChanged += PosView_DataContextChanged;
        }

        private void PosView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PosViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            if (e.OldValue is PosViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PosViewModel.SelectedCategory))
            {
                UpdateCategoryButtonColors();
            }
        }

        private void CategoryButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Category category)
            {
                if (DataContext is PosViewModel viewModel)
                {
                    viewModel.SelectedCategory = category;
                    UpdateCategoryButtonColors();
                }
            }
        }

        private void UpdateCategoryButtonColors()
        {
            if (DataContext is not PosViewModel viewModel) return;

            var blueBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            var greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));

            // Update "All" button
            AllCategoriesButton.Background = viewModel.SelectedCategory == null ? blueBrush : greenBrush;

            // Update category buttons (find them in the visual tree)
            var itemsControl = FindVisualChild<ItemsControl>(this);
            if (itemsControl != null)
            {
                foreach (var item in itemsControl.Items)
                {
                    if (item is Category category)
                    {
                        var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item);
                        var button = FindVisualChild<Button>(container as System.Windows.DependencyObject);
                        if (button != null)
                        {
                            button.Background = (viewModel.SelectedCategory?.Id == category.Id) ? blueBrush : greenBrush;
                        }
                    }
                }
            }
        }

        private static T? FindVisualChild<T>(System.Windows.DependencyObject? parent) where T : System.Windows.DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }
    }
}
