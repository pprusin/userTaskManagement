using System;
using Microsoft.Data.SqlClient;
using Models;

public class TaskService
{
    private readonly AzureConnection _connection = new();

    public void AddTask(User currentUser)
    {
        Console.Write("📌 Tytuł zadania: ");
        string title = Console.ReadLine();

        Console.Write("📝 Opis zadania: ");
        string description = Console.ReadLine();

        Console.Write("👤 Czy przypisać użytkownika? (t/n): ");
        string assign = Console.ReadLine().ToLower();

        int? assignedUserId = null;
        if (assign == "t")
        {
            Console.Write("🔢 Podaj ID użytkownika: ");
            string userInput = Console.ReadLine();
            if (int.TryParse(userInput, out int parsedId))
            {
                assignedUserId = parsedId;
            }
        }

        DateTime? dueDate = null;
        Console.Write("📅 Czy chcesz ustawić datę wykonania? (t/n): ");
        string setDueDate = Console.ReadLine().ToLower();

        if (setDueDate == "t")
        {
            Console.Write("📅 Podaj datę (format: yyyy-MM-dd): ");
            string dateInput = Console.ReadLine();
            if (DateTime.TryParse(dateInput, out DateTime parsedDate))
            {
                dueDate = parsedDate;
            }
            else
            {
                Console.WriteLine("❗ Niepoprawny format daty. Pomijam dueDate.");
            }
        }

        using SqlConnection conn = _connection.GetConnection();
        conn.Open();

        string query = @"
            INSERT INTO dbo.tasks (tytul, taskOpis, taskStatus, taskDataZlozenia, userId, dueDate)
            VALUES (@t, @d, @s, @dt, @u, @due)";

        using SqlCommand cmd = new(query, conn);
        cmd.Parameters.AddWithValue("@t", title);
        cmd.Parameters.AddWithValue("@d", description);
        cmd.Parameters.AddWithValue("@s", "NOWE");
        cmd.Parameters.AddWithValue("@dt", DateTime.Now);
        cmd.Parameters.AddWithValue("@u", assignedUserId.HasValue ? assignedUserId : DBNull.Value);
        cmd.Parameters.AddWithValue("@due", dueDate.HasValue ? dueDate : DBNull.Value);

        cmd.ExecuteNonQuery();

        Console.WriteLine("✅ Zadanie zostało dodane!");
    }

    public void ShowTasks()
    {
        Console.Write("📋 Czy chcesz przefiltrować zadania po statusie? (t/n): ");
        string filterChoice = Console.ReadLine().ToLower();

        string statusFilter = null;
        if (filterChoice == "t")
        {
            Console.Write("🔎 Podaj status (NOWE, W TOKU, ZAKOŃCZONE): ");
            statusFilter = Console.ReadLine().ToUpper();
        }

        using SqlConnection conn = _connection.GetConnection();
        conn.Open();

        string query = @"
            SELECT taskId, tytul, taskStatus, taskDataZlozenia, dueDate
            FROM dbo.tasks";

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query += " WHERE taskStatus = @status";
        }

        using SqlCommand cmd = new(query, conn);
        if (!string.IsNullOrEmpty(statusFilter))
        {
            cmd.Parameters.AddWithValue("@status", statusFilter);
        }

        using SqlDataReader reader = cmd.ExecuteReader();

        Console.WriteLine("\n=== LISTA ZADAŃ ===");
        Console.WriteLine("ID | Tytuł               | Status     | Data złożenia | Termin wykonania");
        Console.WriteLine("--------------------------------------------------------------");

        while (reader.Read())
        {
            int id = reader.GetInt32(0);
            string tytul = reader.GetString(1);
            string status = reader.GetString(2);
            DateTime dataZlozenia = reader.GetDateTime(3);
            string dueDate = reader.IsDBNull(4) ? "-" : reader.GetDateTime(4).ToShortDateString();

            Console.WriteLine($"{id,-3}| {tytul,-20}| {status,-10}| {dataZlozenia.ToShortDateString(),-15}| {dueDate}");
        }
    }



    
}
