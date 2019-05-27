using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Testing
{
    public class TestMainDatabase : MainDatabase
    {
        public TestMainDatabase(DbContextOptions<MainDatabase> options) : base(options)
        {
        }

        protected override void ConfigureWebApiCallback(ModelBuilder modelBuilder)
        {
            base.ConfigureWebApiCallback(modelBuilder);

            modelBuilder.Entity<WebApiCallback>(b =>
            {
                b.Property(e => e.RequestIp).HasConversion(Converters.IPAddressToStringConverter);
                b.Property(e => e.Url).HasConversion(Converters.UriToStringConverter);
            });
        }
    }
}
