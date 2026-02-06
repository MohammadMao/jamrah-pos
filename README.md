# Jamrah POS System

A complete Point of Sale (POS) system for Jamrah restaurant built with WPF, C#, SQLite, and Entity Framework Core.

## Features

### 🔐 User Management
- Secure login with BCrypt password hashing
- Role-based access (Admin/Cashier)
- User account management
- Session tracking

### 📦 Inventory Management
- Category management (add, edit, delete)
- Menu items management with pricing
- Active/inactive status for items
- Arabic RTL interface

### 🛒 Point of Sale
- Quick order creation
- Real-time cart management
- Edit item quantities
- Multiple payment methods (Cash/Card/Other)
- Order voiding capability
- Receipt printing

### 📊 Reports & Analytics
- **Daily Reports** with pagination (7 per page)
  - Total sales and order count
  - Average order value
  - Payment method breakdown
  - Cashier performance
  - Day of week in Arabic
- **Monthly Reports** with year selector
  - All months displayed in descending order
  - Daily breakdown for each month
  - Payment and cashier summaries
  - Arabic Gregorian month names
- Export reports to CSV

### 🧾 Order Management
- View all orders with status
- Filter by date range and payment method
- Order details with itemized breakdown
- Print receipts
- Void orders

### 💰 Multi-Currency Support
- Sudanese Pound (SDG) standardized throughout
- Consistent currency formatting

### 🌐 Localization
- Full Arabic interface with RTL support
- Custom Arabic Gregorian calendar (not Islamic calendar)
- Arabic day and month names

### 🔧 Performance & Reliability
- Automatic database migrations
- Log rotation on startup (keeps last 10 logs, 7-day retention)
- Comprehensive error logging
- Self-contained deployment

## Technology Stack

- **.NET 8.0**: Framework
- **WPF**: UI Framework
- **C#**: Programming Language
- **SQLite**: Database
- **Entity Framework Core 8.0**: ORM
- **BCrypt.Net**: Password hashing

## Default Credentials

- **Username**: admin
- **Password**: admin123


## Installation

1. Extract the published folder to a location (e.g., `C:\JamrahPOS\`)
2. Run `JamrahPOS.exe`
3. Login with default credentials
4. Start using the system!

## Data Storage

- **Database**: `%LocalAppData%\JamrahPOS\JamrahPOS.db`
- **Logs**: `%LocalAppData%\JamrahPOS\app.log`

## Updating

To update the app without losing data:
1. Close the running application
2. Replace only the `JamrahPOS.exe` file
3. Run the new executable
4. All data is preserved automatically

## Building from Source

```bash
dotnet restore
dotnet build
dotnet run
```

## Publishing

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\`

## Architecture

- **MVVM Pattern**: Clean separation of concerns
- **Repository Pattern**: Database access abstraction
- **Service Layer**: Business logic isolation
- **RTL Support**: Native Arabic interface

## Screenshots
<img width="1919" height="1002" alt="Screenshot 2026-02-06 082732" src="https://github.com/user-attachments/assets/1cfda277-6924-4cc0-b22a-face0f85ddc4" />
<img width="1919" height="1002" alt="Screenshot 2026-02-06 082836" src="https://github.com/user-attachments/assets/560efc67-902d-4853-9694-c71104f082d2" />
