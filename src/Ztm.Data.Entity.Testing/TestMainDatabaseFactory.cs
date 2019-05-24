using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts;

namespace Ztm.Data.Entity.Testing
{
    public sealed class TestMainDatabaseFactory : IMainDatabaseFactory, IDisposable
    {
        readonly SqliteConnection connection;

        public TestMainDatabaseFactory()
        {
            this.connection = new SqliteConnection("DataSource=:memory:");

            try
            {
                this.connection.Open();
            }
            catch
            {
                this.connection.Dispose();
                throw;
            }
        }

        public MainDatabase CreateDbContext()
        {
            var builder = new DbContextOptionsBuilder<MainDatabase>();

            builder.UseSqlite(this.connection);

            var context = new TestMainDatabase(builder.Options);

            try
            {
                context.Database.EnsureCreated();
            }
            catch
            {
                context.Dispose();
                throw;
            }

            return context;
        }

        public void Dispose()
        {
            this.connection.Dispose();
        }
    }
}
