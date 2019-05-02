namespace Ztm.Configuration
{
    public class DatabaseConfiguration
    {
        public MainDatabaseConfiguration Main { get; set; }
    }

    public class MainDatabaseConfiguration
    {
        public string ConnectionString { get; set; }
    }
}
