using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Contexts
{
    public class MainDatabase : DbContext
    {
        readonly NamingConvention naming;

        public MainDatabase(DbContextOptions<MainDatabase> options, NamingConvention naming = NamingConvention.Pascal)
            : base(options)
        {
            this.naming = naming;
        }

        public DbSet<WebApiCallback> WebApiCallbacks { get; set; }

        protected virtual void ConfigureWebApiCallback(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WebApiCallback>(b =>
            {
                b.Property(e => e.Id).IsRequired().ValueGeneratedNever();
                b.Property(e => e.RequestIp).IsRequired();
                b.Property(e => e.RequestTime).IsRequired();
                b.Property(e => e.TransactionId).HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.Url).IsRequired();
                b.HasKey(e => e.Id);
                b.HasIndex(e => e.TransactionId);

                switch (this.naming)
                {
                    case NamingConvention.Snake:
                        b.Property(e => e.Id).HasColumnName("id");
                        b.Property(e => e.RequestIp).HasColumnName("request_ip");
                        b.Property(e => e.RequestTime).HasColumnName("request_time");
                        b.Property(e => e.TransactionId).HasColumnName("transaction_id");
                        b.Property(e => e.Url).HasColumnName("url");
                        b.ToTable("webapi_callback");
                        break;
                }
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureWebApiCallback(modelBuilder);
        }
    }
}
