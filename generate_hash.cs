#if false
using BCrypt.Net;

class Program
{
    static void Main()
    {
        string password = "admin123";
        string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");
        
        // Test verification
        bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine($"Verify result: {isValid}");
    }
}
#endif
