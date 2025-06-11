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

        string insertQuery = @"
            INSERT INTO dbo.userIdentity (userLogin, userPassword, userEmail)
            VALUES (@login, @password, @email)";

        using SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
        insertCmd.Parameters.AddWithValue("@login", login);
        insertCmd.Parameters.AddWithValue("@password", hashedPassword);
        insertCmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);

        insertCmd.ExecuteNonQuery();

        Console.WriteLine("✅ Rejestracja zakończona sukcesem!");
    }

    public User LoginAndReturnUser()
    {
        Console.Write("🔑 Login: ");
        string login = Console.ReadLine();

        Console.Write("🔑 Hasło: ");
        string password = ReadPassword();

        using SqlConnection conn = _connection.GetConnection();
        conn.Open();

        string query = "SELECT userId, userPassword, userEmail FROM dbo.userIdentity WHERE userLogin = @login";
        using SqlCommand cmd = new(query, conn);
        cmd.Parameters.AddWithValue("@login", login);

        using SqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            string storedHash = reader.GetString(1);
            string inputHash = HashPassword(password, storedHash);

            if (storedHash == inputHash)
            {
                Console.WriteLine("✅ Logowanie udane!");

                return new User
                {
                    UserId = reader.GetInt32(0),
                    Login = login,
                    PasswordHash = storedHash,
                    Email = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
            }
        }

        Console.WriteLine("❌ Logowanie nieudane.");
        return null;
    }


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

    private string HashPassword(string password, string storedHash)
    {
        byte[] hashBytes = Convert.FromBase64String(storedHash);
        byte[] salt = new byte[16];
        Array.Copy(hashBytes, 0, salt, 0, 16);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);

        byte[] combinedHash = new byte[48];
        Array.Copy(salt, 0, combinedHash, 0, 16);
        Array.Copy(hash, 0, combinedHash, 16, 32);

        return Convert.ToBase64String(combinedHash);
    }
}
