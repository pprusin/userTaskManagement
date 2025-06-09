class Program
{
    static void Main()
    {
        var db = new AzureConnection();
        db.TestConnection();

        var auth = new AuthService();
        auth.Register();
    }

}
