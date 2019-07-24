using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ztm.Data.Entity.Postgres
{
    public class MainDatabaseDesignTimeFactory : IDesignTimeDbContextFactory<MainDatabase>
    {
        public MainDatabase CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<Ztm.Data.Entity.Contexts.MainDatabase>();
            var connectionString = Environment.GetEnvironmentVariable("ZTM_MAIN_DATABASE");

            if (connectionString == null)
            {
                throw new Exception("No ZTM_MAIN_DATABASE environment variable is set.");
            }

            optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.ReplaceService<IRelationalTypeMappingSource, TypeMappingSource>();

            return new MainDatabase(optionsBuilder.Options);
        }
    }
}
