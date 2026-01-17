using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JamrahPOS.Models
{
    /// <summary>
    /// Represents an item in the shopping cart (temporary order)
    /// </summary>
    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _unitPrice;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; } // Store original menu price
        
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                    OnPropertyChanged(nameof(HasDiscount));
                    OnPropertyChanged(nameof(DiscountAmount));
                }
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal TotalPrice => UnitPrice * Quantity;
        public bool HasDiscount => UnitPrice < OriginalPrice;
        public decimal DiscountAmount => (OriginalPrice - UnitPrice) * Quantity;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
