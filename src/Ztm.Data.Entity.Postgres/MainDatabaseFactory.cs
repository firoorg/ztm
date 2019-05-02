using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ztm.Configuration;
using Ztm.Data.Entity.Contexts;

namespace Ztm.Data.Entity.Postgres
{
    public class MainDatabaseFactory : IMainDatabaseFactory
    {
        readonly MainDatabaseConfiguration config;

        public MainDatabaseFactory(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config.GetDatabaseSection().Main;
        }

        public Ztm.Data.Entity.Contexts.MainDatabase CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<Ztm.Data.Entity.Contexts.MainDatabase>();

            optionsBuilder.UseNpgsql(this.config.ConnectionString);

            return new MainDatabase(optionsBuilder.Options);
        }
    }
}
