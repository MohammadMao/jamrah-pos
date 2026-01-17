# Jamrah POS System

A modern Point of Sale (POS) system for restaurants built with WPF, C#, SQLite, and Entity Framework Core.

## Project Structure

```
JamrahPOS/
├── Models/          # Data models and entities
├── ViewModels/      # MVVM ViewModels
│   ├── BaseViewModel.cs
│   └── MainViewModel.cs
├── Views/           # WPF Views (XAML)
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
├── Services/        # Business logic and services
├── Data/            # Database context and configurations
├── Helpers/         # Utility classes
│   └── RelayCommand.cs
├── App.xaml
└── App.xaml.cs
```

## Architecture

- **MVVM Pattern**: Separation of concerns with Model-View-ViewModel
- **BaseViewModel**: Implements INotifyPropertyChanged for property binding
- **RelayCommand**: ICommand implementation for XAML command binding
- **Entity Framework Core**: ORM for database access
- **SQLite**: Lightweight database for data storage

## Technology Stack

- **.NET 8.0**: Framework
- **WPF**: UI Framework
- **C#**: Programming Language
- **SQLite**: Database
- **Entity Framework Core 8.0**: ORM

## NuGet Packages

- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.Sqlite (8.0.0)
- Microsoft.EntityFrameworkCore.Tools (8.0.0)

## Build Instructions

This project requires Windows to build and run (WPF is Windows-only).

```bash
dotnet restore
dotnet build
dotnet run
```

## Development Guidelines

1. **Keep UI logic in ViewModels** - Views should only contain XAML markup
2. **Use data binding** - Leverage WPF's powerful binding system
3. **Follow MVVM** - Maintain clean separation between layers
4. **Use RelayCommand** - For all button clicks and user actions
5. **Inherit from BaseViewModel** - For all ViewModels to get property change notification

## Database Schema

### Entities

**User**
- Id (PK)
- Username (unique)
- PasswordHash
- Role (Admin / Cashier)
- IsActive (soft delete flag)

**Category**
- Id (PK)
- Name (in Arabic)

**MenuItem**
- Id (PK)
- Name (in Arabic)
- Price (decimal)
- CategoryId (FK)
- IsActive (soft delete flag)

**Order**
- Id (PK)
- OrderNumber (unique)
- OrderDateTime
- TotalAmount (decimal)
- PaymentMethod (Cash/Card)
- CashierId (FK)
- IsVoided (soft delete flag)

**OrderItem**
- Id (PK)
- OrderId (FK)
- MenuItemId (FK)
- Quantity
- UnitPrice (decimal)
- TotalPrice (decimal)

### Relationships

- User 1:N Order (One user can create many orders)
- Category 1:N MenuItem (One category contains many menu items)
- MenuItem 1:N OrderItem (One menu item can be in many order items)
- Order 1:N OrderItem (One order contains many order items)

### Database Location

SQLite database file is stored at:
`%LocalAppData%/JamrahPOS/JamrahPOS.db`

## Seed Data

The database is automatically seeded with:
- Default admin user (username: admin, password: admin123)
- Sample categories in Arabic:
  - المشروبات (Drinks)
  - المقبلات (Appetizers)
  - الأطباق الرئيسية (Main Dishes)
  - الحلويات (Desserts)

## Notes

- **UI Language**: All UI elements should be in Arabic for the Arab restaurant
- **Soft Delete**: Entities use IsActive/IsVoided flags instead of hard deletion
- **Password Security**: Current implementation uses plain text (TODO: implement proper hashing with BCrypt)

## Next Steps

Continue with subsequent parts to implement:
- User authentication and authorization
- Product/menu management
- Order processing
- Payment handling
- Reporting features
