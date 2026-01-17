# Authentication & Security Documentation

## Authentication Flow

1. **Application Startup** → LoginWindow is displayed
2. **User enters credentials** → Username and Password
3. **Authentication** → Credentials verified against database
4. **Session Created** → User stored in SessionService
5. **Main Window Opens** → User can access the system

## Default Credentials

- **Username:** admin
- **Password:** admin123
- **Role:** Admin

## Password Security

- Passwords are hashed using **BCrypt** algorithm
- BCrypt work factor: 11 (2^11 iterations)
- Passwords are never stored in plain text
- Hash example: `$2a$11$8K1p/a0dL3LHAkH4v.9tCe8sJlUgz1qJ3x5t6ygWdZKvIYZp3qYXK`

## User Roles

### Admin (مدير)
- Full system access
- Can manage users
- Can manage menu items
- Can view all reports
- Can configure system settings

### Cashier (كاشير)
- Create and process orders
- View menu items
- Process payments
- Limited report access

## Role-Based Access Control

### Using SessionService
```csharp
// Check if user is logged in
if (SessionService.Instance.IsLoggedIn)
{
    // User is authenticated
}

// Check if user is admin
if (SessionService.Instance.IsAdmin)
{
    // User has admin privileges
}

// Check if user is cashier
if (SessionService.Instance.IsCashier)
{
    // User has cashier privileges
}
```

### Using AuthorizationHelper
```csharp
// Require admin with custom message
if (AuthorizationHelper.RequireAdmin("إدارة المستخدمين"))
{
    // User is admin, proceed with action
}

// Get current user information
var username = AuthorizationHelper.GetCurrentUserName();
var role = AuthorizationHelper.GetCurrentUserRoleArabic();
```

## Services

### AuthenticationService
Handles user authentication and password management:
- `AuthenticateAsync(username, password)` - Authenticate user
- `HashPassword(password)` - Hash a password
- `VerifyPassword(password, hash)` - Verify password
- `ChangePasswordAsync(userId, oldPassword, newPassword)` - Change password

### SessionService
Manages current user session (Singleton):
- `Instance.CurrentUser` - Get current logged-in user
- `Instance.IsLoggedIn` - Check if user is logged in
- `Instance.IsAdmin` - Check if user is admin
- `Instance.IsCashier` - Check if user is cashier
- `Login(user)` - Store user in session
- `Logout()` - Clear session

## Security Features

✅ **Password Hashing** - BCrypt with salt
✅ **Session Management** - Singleton pattern
✅ **Role-Based Access** - Admin/Cashier separation
✅ **Secure Login Flow** - Database-backed authentication
✅ **Logout Functionality** - Clean session termination
✅ **Arabic UI** - Full RTL support

## Future Enhancements

- [ ] Password complexity requirements
- [ ] Account lockout after failed attempts
- [ ] Session timeout
- [ ] Audit logging for user actions
- [ ] Password reset functionality
- [ ] Two-factor authentication (2FA)

## UI Components

### LoginWindow
- Arabic interface (RTL)
- Username input
- Password input (masked)
- Error messages in Arabic
- Login and Exit buttons

### MainWindow Header
- Displays current user name
- Shows user role (مدير/كاشير)
- Logout button
- Confirmation dialog for logout

## Error Messages (Arabic)

- `"الرجاء إدخال اسم المستخدم"` - Please enter username
- `"الرجاء إدخال كلمة المرور"` - Please enter password
- `"اسم المستخدم أو كلمة المرور غير صحيحة"` - Invalid username or password
- `"هذه الميزة متاحة للمدير فقط"` - This feature is for admin only
- `"هل تريد تسجيل الخروج؟"` - Do you want to logout?
