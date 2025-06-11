using System;
using Models;

class Program
{
    static void Main()
    {
        var db = new AzureConnection();
        db.TestConnection();

        var auth = new AuthService();
        var isLogged = false;
        User currentUser = null;

        while (!isLogged)
        {
            Console.WriteLine("\n=== MENU ===");
            Console.WriteLine("1. Rejestracja");
            Console.WriteLine("2. Logowanie");
            Console.WriteLine("3. Wyjście");
            Console.Write("Wybierz opcję (1-3): ");

            string choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    auth.Register();
                    break;
                case "2":
                    currentUser = auth.LoginAndReturnUser();
                    isLogged = currentUser != null;
                    break;
                case "3":
                    Console.WriteLine("👋 Zamykam aplikację.");
                    return;
                default:
                    Console.WriteLine("❗ Nieprawidłowy wybór. Spróbuj ponownie.");
                    break;
            }
        }

        var taskService = new TaskService();
        bool continueWorking = true;

        while (continueWorking)
        {
            Console.WriteLine("\n=== ZALOGOWANY ===");
            Console.WriteLine("1. ➕ Dodaj zadanie");
            Console.WriteLine("2. 📋 Wyświetl zadania");
            Console.WriteLine("3. 🚪 Wyloguj");
            Console.Write("Wybierz opcję (1-3): ");
            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    taskService.AddTask(currentUser);
                    break;
                case "2":
                    taskService.ShowTasks();
                    break;
                case "3":
                    Console.WriteLine("✅ Wylogowano.");
                    continueWorking = false;
                    break;
                default:
                    Console.WriteLine("❗ Nieprawidłowa opcja.");
                    break;
            }
        }
    }
}
