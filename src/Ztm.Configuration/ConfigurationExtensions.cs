using Microsoft.Extensions.Configuration;

namespace Ztm.Configuration
{
    public static class ConfigurationExtensions
    {
        public static DatabaseConfiguration GetDatabaseSection(this IConfiguration config)
        {
            return config.GetSection("Database").Get<DatabaseConfiguration>();
        }

        public static ZcoinConfiguration GetZcoinSection(this IConfiguration config)
        {
            return config.GetSection("Zcoin").Get<ZcoinConfiguration>();
        }
    }
}
