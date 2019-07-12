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

            modelBuilder.Entity<Block>(b =>
            {
                b.Property(e => e.Hash).HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.MerkleRoot).HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.MtpHashValue).HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.Reserved1).HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.Reserved2).HasConversion(Converters.UInt256ToBytesConverter);
            });

            modelBuilder.Entity<BlockTransaction>(b =>
            {
                b.Property(e => e.BlockHash).HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.TransactionHash).HasConversion(Converters.UInt256ToBytesConverter);
            });

            modelBuilder.Entity<Input>(b =>
            {
                b.Property(e => e.TransactionHash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.OutputHash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
            });

            modelBuilder.Entity<Output>(b =>
            {
                b.Property(e => e.TransactionHash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
            });

            modelBuilder.Entity<Transaction>(b =>
            {
                b.Property(e => e.Hash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
            });

            modelBuilder.Entity<WatchingBlock>(b =>
            {
                b.Property(e => e.Hash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
            });

            modelBuilder.Entity<WatchingTransaction>(b =>
            {
                b.Property(e => e.Hash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
            });

            modelBuilder.Entity<WebApiCallback>(b =>
            {
                b.Property(e => e.TransactionId).HasConversion(Converters.UInt256ToBytesConverter);
            });
        }
    }
}
