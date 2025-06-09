using System;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Models;

public class AuthService
{
    private readonly AzureConnection _connection;

    public AuthService()
    {
        _connection = new AzureConnection();
    }

    public void Register()
    {
        Console.Write("🔐 Podaj login: ");
        string login = Console.ReadLine();

        Console.Write("🔐 Podaj hasło: ");
        string password = ReadPassword();

        Console.Write("📧 Podaj e-mail (opcjonalnie): ");
        string email = Console.ReadLine();

        // Sprawdzenie, czy login już istnieje
        using SqlConnection conn = _connection.GetConnection();
        conn.Open();

        string checkQuery = "SELECT COUNT(*) FROM dbo.userIdentity WHERE userLogin = @login";
        using SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
        checkCmd.Parameters.AddWithValue("@login", login);

        int exists = (int)checkCmd.ExecuteScalar();
        if (exists > 0)
        {
            Console.WriteLine("❌ Taki login już istnieje.");
            return;
        }

        string hashedPassword = HashPassword(password);

        string insertQuery = "INSERT INTO dbo.userIdentity (userLogin, userPassword, userEmail) VALUES (@login, @password, @email)";
        using SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
        insertCmd.Parameters.AddWithValue("@login", login);
        insertCmd.Parameters.AddWithValue("@password", hashedPassword);
        insertCmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);

        insertCmd.ExecuteNonQuery();

        Console.WriteLine("✅ Rejestracja zakończona sukcesem!");
    }

    // Pomocnicza funkcja do wczytania hasła bez wyświetlania
    private string ReadPassword()
    {
        StringBuilder password = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) break;

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        Console.WriteLine();
        return password.ToString();
    }

    // Hashowanie hasła z użyciem SHA256 + salt
    private string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);

        byte[] hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }
}
