class Program
{
    static void Main()
    {
        var db = new AzureConnection();
        db.TestConnection();
    }
}
