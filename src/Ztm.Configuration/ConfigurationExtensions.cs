using Microsoft.Extensions.Configuration;

namespace Ztm.Configuration
{
    public static class ConfigurationExtensions
    {
        public static ZcoinConfiguration GetZcoinSection(this IConfiguration config)
        {
            return config.GetSection("Zcoin").Get<ZcoinConfiguration>();
        }
    }
}
