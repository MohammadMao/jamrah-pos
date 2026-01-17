# PART 5 – Order Editing & Price Override (Discount Logic)

## Overview
This part implements the ability for cashiers to edit cart items, including quantity adjustments and price overrides for discounts.

## Features Implemented

### 1. Cart Item Editing
- Edit button (✏ تعديل) added to each cart item
- Opens EditItemDialog for modifying quantity and price
- Real-time discount calculation display

### 2. Price Override (Discount Feature)
- Unit price can be manually adjusted
- Original menu price is preserved in `CartItem.OriginalPrice`
- `CartItem.UnitPrice` becomes editable for applying discounts
- Discount amount and percentage automatically calculated and displayed

### 3. Updated Models

#### CartItem (Models/CartItem.cs)
- Implements `INotifyPropertyChanged` for UI binding
- Properties:
  - `OriginalPrice`: Stores the original menu item price (immutable)
  - `UnitPrice`: Current price (editable for discounts)
  - `HasDiscount`: Computed property that returns true if UnitPrice < OriginalPrice
  - `DiscountAmount`: Computed property that returns OriginalPrice - UnitPrice
  - `TotalPrice`: Computed property (Quantity × UnitPrice)

### 4. Edit Item Dialog (Views/EditItemDialog.xaml)

#### Features:
- **Item Name Display**: Shows the selected item name
- **Quantity Controls**: 
  - +/- buttons for easy quantity adjustment
  - Numeric text input with validation
  - Minimum quantity: 1
- **Price Override**:
  - Editable price field (decimal input validation)
  - Shows original menu price as reference
  - Real-time discount display when price is reduced
  - Discount shown as amount and percentage
- **Actions**:
  - Save: Updates cart item with new values
  - Cancel: Discards changes

#### Arabic UI Elements:
- Item name: displays cart item name
- Original price label: "السعر الأصلي"
- Quantity label: "الكمية"
- Price label: "السعر"
- Discount display: "خصم: X ريال (Y%)"
- Save button: "حفظ"
- Cancel button: "إلغاء"

### 5. Service Updates

#### OrderService (Services/OrderService.cs)
New methods added:
- `UpdatePrice(CartItem item, decimal newPrice)`: Updates unit price for discount
- `UpdateCartItem(CartItem item, int newQuantity, decimal newPrice)`: Updates both quantity and price
- Validates that price cannot be negative
- Removes item if quantity becomes 0 or less

### 6. ViewModel Updates

#### PosViewModel (ViewModels/PosViewModel.cs)
- `EditItemCommand`: Opens EditItemDialog for selected cart item
- `EditItem(CartItem item)`: Method that handles the edit flow
- Calls `OrderService.UpdateCartItem()` when changes are saved
- Refreshes cart display after editing

### 7. View Updates

#### PosView (Views/PosView.xaml)
Cart item display enhanced with:
- Edit button (✏ تعديل) for each item
- Discount amount display (shown when HasDiscount is true)
- Orange color (#FF6F00) for discount text
- Edit button styling: Blue background (#2196F3)

## User Flow

### Editing an Item:
1. Cashier clicks "✏ تعديل" button on a cart item
2. EditItemDialog opens showing:
   - Item name
   - Original price
   - Current quantity (editable)
   - Current unit price (editable)
3. Cashier can:
   - Adjust quantity using +/- buttons or direct input
   - Change price to apply discount
   - See discount amount/percentage in real-time
4. Clicking "حفظ" (Save):
   - Updates cart item with new values
   - Recalculates total
   - Closes dialog
5. Clicking "إلغاء" (Cancel):
   - Discards all changes
   - Closes dialog

### Discount Display:
- When item has a discount (UnitPrice < OriginalPrice):
  - Shows "خصم: X ريال" below item details
  - Displayed in orange color for visibility
  - Only visible when discount is applied

## Technical Details

### Data Binding
- CartItem implements INotifyPropertyChanged
- Properties notify UI of changes automatically
- ObservableCollection<CartItem> updates cart view reactively

### Validation
- Quantity: Must be positive integer (minimum 1)
- Price: Must be non-negative decimal
- Regex validation for numeric inputs
- Error messages in Arabic for invalid inputs

### Price Calculation
```csharp
// Discount is calculated as:
decimal discount = OriginalPrice - UnitPrice;
decimal percentage = (OriginalPrice > 0) ? (discount / OriginalPrice) * 100 : 0;

// Total price per item:
decimal totalPrice = Quantity * UnitPrice;
```

### Original Price Preservation
- Menu item prices remain unchanged in database
- `OriginalPrice` in CartItem stores menu price
- Discounts only affect current cart, not menu items
- Each order item saves actual unit price used

## Files Created/Modified

### New Files:
- `Views/EditItemDialog.xaml`: Edit dialog UI
- `Views/EditItemDialog.xaml.cs`: Edit dialog logic
- `PART5_ORDER_EDITING.md`: This documentation

### Modified Files:
- `Models/CartItem.cs`: Added INotifyPropertyChanged, OriginalPrice, discount properties
- `Services/OrderService.cs`: Added UpdatePrice() and UpdateCartItem() methods
- `ViewModels/PosViewModel.cs`: Added EditItemCommand and EditItem() method
- `Views/PosView.xaml`: Added edit button and discount display to cart items

## Benefits

1. **Flexible Pricing**: Cashiers can apply discounts without manager approval for small amounts
2. **Price Transparency**: Original price always visible, discount clearly shown
3. **Audit Trail**: Order items store actual unit price used for later review
4. **User-Friendly**: Simple +/- buttons and clear Arabic interface
5. **Real-time Feedback**: Discount calculations update instantly as price changes

## Future Enhancements (Optional)

- Discount reason field (e.g., "عميل دائم", "منتج تالف")
- Maximum discount percentage limit based on user role
- Discount approval workflow for large amounts
- Discount statistics and reporting
- Predefined discount buttons (10%, 20%, 50%)
