using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Ztm.Data.Entity.Postgres.Tests
{
    public sealed class MainDatabaseFixture : IDisposable
    {
        readonly DbConnection connection;
        readonly DbContextOptionsBuilder<Ztm.Data.Entity.Contexts.MainDatabase> optionsBuilder;

        public MainDatabaseFixture()
        {
            var builder = new ConfigurationBuilder();

            var connectionString = Environment.GetEnvironmentVariable("ZTM_MAIN_DATABASE");
            if (connectionString == null)
            {
                throw new Exception("No ZTM_MAIN_DATABASE environment variable is set.");
            }

            this.connection  = new NpgsqlConnection(connectionString);

            this.optionsBuilder = new DbContextOptionsBuilder<Ztm.Data.Entity.Contexts.MainDatabase>();
            this.optionsBuilder.UseNpgsql(this.connection);
            this.optionsBuilder.UseUInt256TypeMappingSource();
        }

        public MainDatabase CreateMainDatabase()
        {
            return new MainDatabase(optionsBuilder.Options);
        }

        public void CleanUp()
        {
            // Truncate all tables in schema public except __EFMigrationsHistory
            var tables = this.ExecuteSql("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory';")
                             .Select(r => r[0] as string);

            using (var db = this.CreateMainDatabase())
            {
                var query = "TRUNCATE TABLE " + string.Join(", ", tables.Select(t => '"' + t + '"'));
                db.Database.ExecuteSqlCommand(query); // lgtm [cs/second-order-sql-injection]
            }
        }

        public void Dispose()
        {
            this.connection.Dispose();
        }

        public IEnumerable<IDataRecord> ExecuteSql(string rawSql)
        {
            using (var db = this.CreateMainDatabase())
            using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = rawSql;
                db.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        yield return result;
                    }
                }

                db.Database.CloseConnection();
            }
        }
    }
}