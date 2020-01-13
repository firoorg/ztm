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

        public MainDatabaseFixture()
        {
            var builder = new ConfigurationBuilder();

            var connectionString = Environment.GetEnvironmentVariable("ZTM_MAIN_DATABASE");

            if (connectionString == null)
            {
                throw new Exception("No ZTM_MAIN_DATABASE environment variable is set.");
            }

            this.connection  = new NpgsqlConnection(connectionString);
        }

        public MainDatabase CreateMainDatabase()
        {
            var optionsBuilder = new DbContextOptionsBuilder<Ztm.Data.Entity.Contexts.MainDatabase>();

            optionsBuilder.UseNpgsql(this.connection);
            optionsBuilder.UseUInt256TypeMappingSource();

            return new MainDatabase(optionsBuilder.Options);
        }

        public void CleanUp()
        {
            using (var db = this.CreateMainDatabase())
            using (var tx = db.Database.BeginTransaction())
            using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                // Truncate all tables in schema public except __EFMigrationsHistory
                var tables = new List<string>();

                command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory';";
                db.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        var record = (IDataRecord)result;
                        var table = record[0] as string;
                        tables.Add(table);
                    }
                }

                db.Database.CloseConnection();

                var query = "TRUNCATE TABLE " + string.Join(", ", tables.Select(t => '"' + t + '"'));
                db.Database.ExecuteSqlCommand(query); // lgtm [cs/second-order-sql-injection]

                tx.Commit();
            }
        }

        public void Dispose()
        {
            this.connection.Close();
        }
    }
}