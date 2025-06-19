using System;
using Microsoft.Data.SqlClient;

public class AzureConnection
{
    private readonly string _connectionString;

    public AzureConnection()
    {
        _connectionString = "Server=" + Environment.GetEnvironmentVariable("SERVER") + ";" +
                            "Initial Catalog=userTaskManagement;" +
                            "Persist Security Info=False;" +
                            "User ID=" + Environment.GetEnvironmentVariable("USER") + ";" +
                            "Password=" + Environment.GetEnvironmentVariable("PASS") + ";" +
                            "MultipleActiveResultSets=False;" +
                            "Encrypt=True;" +
                            "TrustServerCertificate=False;" +
                            "Connection Timeout=30;";
    }

    public SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public void TestConnection()
    {
        try
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            Console.WriteLine("✅ Połączenie z bazą danych powiodło się.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Błąd połączenia:");
            Console.WriteLine(ex.Message);
        }
    }
}
