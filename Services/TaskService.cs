using System;
using Microsoft.Data.SqlClient;
using Models;
using programowanie_zaawansowane.Services;

public class TaskService
{
    private readonly AzureConnection _connection = new();
    private readonly EmailSender _emailSender = new();

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

        _emailSender.SendEmail(
            currentUser.Email,
            "Nowe zadanie utworzone",
            $"Użytkownik: {currentUser.Login}\nTytuł: {title}\nOpis: {description}"
);

    }

    public void ShowTasks()
    {
        using var connection = _connection.GetConnection();
        connection.Open();

        var command = new SqlCommand("SELECT * FROM Tasks", connection);
        var reader = command.ExecuteReader();

        Console.WriteLine("\nID | Tytuł               | Status     | Data Utworzenia      | Użytkownik | Due Date           | Opis");
        Console.WriteLine(new string('-', 110));

        while (reader.Read())
        {
            int id = (int)reader["taskId"];
            string title = reader["tytul"].ToString();
            string status = reader["taskStatus"].ToString();
            DateTime createdAt = (DateTime)reader["taskDataZlozenia"];
            string user = reader["userId"]?.ToString() ?? "brak";
            DateTime? dueDate = reader["dueDate"] as DateTime?;
            string description = reader["taskOpis"]?.ToString() ?? "";

            if (description.Length > 100)
            {
                description = description.Substring(0, 100) + "...";
            }

            Console.WriteLine($"{id,-3} | {title,-20} | {status,-10} | {createdAt:yyyy-MM-dd HH:mm} | {user,-10} | {dueDate?.ToString("yyyy-MM-dd"),-20} | {description}");
        }

        reader.Close();
    }

    public void EditTaskStatus()

    {
        Console.Write("🔢 Podaj ID zadania do zmiany statusu: ");
        string input = Console.ReadLine();

        if (!int.TryParse(input, out int taskId))
        {
            Console.WriteLine("❗ Nieprawidłowy format ID.");
            return;
        }

        using var connection = _connection.GetConnection();
        connection.Open();

        // Pobranie aktualnego statusu
        string getStatusQuery = "SELECT taskStatus FROM Tasks WHERE taskId = @id";
        using var getCmd = new SqlCommand(getStatusQuery, connection);
        getCmd.Parameters.AddWithValue("@id", taskId);

        var currentStatusObj = getCmd.ExecuteScalar();
        if (currentStatusObj == null)
        {
            Console.WriteLine("❗ Nie znaleziono zadania o podanym ID.");
            return;
        }

        string currentStatus = currentStatusObj.ToString();
        string newStatus = currentStatus switch
        {
            "NOWE" => "W TOKU",
            "W TOKU" => "ZAKOŃCZONE",
            "ZAKOŃCZONE" => "ZAKOŃCZONE",
            _ => null
        };

        if (newStatus == null || newStatus == currentStatus)
        {
            Console.WriteLine("⚠️ Status nie został zmieniony.");
            return;
        }

        // Aktualizacja statusu
        string updateQuery = "UPDATE Tasks SET taskStatus = @newStatus WHERE taskId = @id";
        using var updateCmd = new SqlCommand(updateQuery, connection);
        updateCmd.Parameters.AddWithValue("@newStatus", newStatus);
        updateCmd.Parameters.AddWithValue("@id", taskId);

        int affected = updateCmd.ExecuteNonQuery();
        if (affected > 0)
        {
            Console.WriteLine($"✅ Status zadania został zmieniony na: {newStatus}");
        }
        else
        {
            Console.WriteLine("❌ Wystąpił błąd podczas aktualizacji.");
        }
    }

    public void SearchTasks()
    {
        Console.WriteLine("\n🔍 Wybierz filtr wyszukiwania:");
        Console.WriteLine("1. Status");
        Console.WriteLine("2. ID użytkownika");
        Console.WriteLine("3. Fraza w tytule lub opisie");
        Console.Write("Wybór (1-3): ");
        string choice = Console.ReadLine();

        string query = "SELECT * FROM Tasks WHERE 1=1 ";
        List<SqlParameter> parameters = new();

        switch (choice)
        {
            case "1":
                Console.Write("Podaj status (NOWE / W TOKU / ZAKOŃCZONE): ");
                string status = Console.ReadLine();
                query += "AND taskStatus = @status ";
                parameters.Add(new SqlParameter("@status", status));
                break;

            case "2":
                Console.Write("Podaj ID użytkownika: ");
                string uid = Console.ReadLine();
                if (int.TryParse(uid, out int userId))
                {
                    query += "AND userId = @uid ";
                    parameters.Add(new SqlParameter("@uid", userId));
                }
                else
                {
                    Console.WriteLine("❗ Niepoprawny format ID.");
                    return;
                }
                break;

            case "3":
                Console.Write("Wpisz frazę: ");
                string keyword = Console.ReadLine();
                query += "AND (tytul LIKE @kw OR taskOpis LIKE @kw) ";
                parameters.Add(new SqlParameter("@kw", $"%{keyword}%"));
                break;

            default:
                Console.WriteLine("❗ Nieprawidłowy wybór.");
                return;
        }

        using var connection = _connection.GetConnection();
        connection.Open();

        using var command = new SqlCommand(query, connection);
        foreach (var p in parameters)
            command.Parameters.Add(p);

        using var reader = command.ExecuteReader();

        Console.WriteLine("\nID | Tytuł               | Status     | Data Utworzenia      | Użytkownik | Due Date           | Opis");
        Console.WriteLine(new string('-', 110));

        while (reader.Read())
        {
            int id = (int)reader["taskId"];
            string title = reader["tytul"].ToString();
            string status = reader["taskStatus"].ToString();
            DateTime createdAt = (DateTime)reader["taskDataZlozenia"];
            string user = reader["userId"]?.ToString() ?? "brak";
            DateTime? dueDate = reader["dueDate"] as DateTime?;
            string description = reader["taskOpis"]?.ToString() ?? "";

            if (description.Length > 100)
                description = description.Substring(0, 100) + "...";

            Console.WriteLine($"{id,-3} | {title,-20} | {status,-10} | {createdAt:yyyy-MM-dd HH:mm} | {user,-10} | {dueDate?.ToString("yyyy-MM-dd"),-20} | {description}");
        }

    }
}
