using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Postgres
{
    public class MainDatabase : Ztm.Data.Entity.Contexts.MainDatabase
    {
        public MainDatabase(DbContextOptions<Ztm.Data.Entity.Contexts.MainDatabase> options) : base(options)
        {
        }

        protected override void ConfigureWebApiCallback(ModelBuilder modelBuilder)
        {
            base.ConfigureWebApiCallback(modelBuilder);

            modelBuilder.Entity<WebApiCallback>(b =>
            {
                b.Property(e => e.Url).HasConversion(Converters.UriToStringConverter);
            });
        }
    }
}
