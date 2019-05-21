using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Contexts
{
    public class MainDatabase : DbContext
    {
        public MainDatabase(DbContextOptions<MainDatabase> options) : base(options)
        {
        }

        public DbSet<Block> Blocks { get; set; }

        public DbSet<Input> Inputs { get; set; }

        public DbSet<Output> Outputs { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<WebApiCallback> WebApiCallbacks { get; set; }

        protected virtual void ConfigureBlock(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Block>(b =>
            {
                b.Property(e => e.Height).IsRequired();
                b.Property(e => e.Hash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.Version).IsRequired();
                b.Property(e => e.Bits).IsRequired().HasConversion(Converters.TargetToInt64);
                b.Property(e => e.Nonce).IsRequired();
                b.Property(e => e.Time).IsRequired();
                b.Property(e => e.MerkleRoot).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);

                b.HasKey(e => e.Height);
                b.HasAlternateKey(e => e.Hash);
            });
        }

        protected virtual void ConfigureInput(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Input>(b =>
            {
                b.Property(e => e.TransactionHash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.Index).IsRequired();
                b.Property(e => e.OutputHash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.OutputIndex).IsRequired();
                b.Property(e => e.Script).IsRequired().HasConversion(Converters.ScriptToBytesConverter);
                b.Property(e => e.Sequence).IsRequired();

                b.HasKey(e => new { e.TransactionHash, e.Index });
                b.HasIndex(e => new { e.OutputHash, e.OutputIndex });
                b.HasOne(e => e.Transaction)
                 .WithMany(e => e.Inputs)
                 .HasForeignKey(e => e.TransactionHash)
                 .HasPrincipalKey(e => e.Hash)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(e => e.Output)
                 .WithMany(e => e.Inputs)
                 .HasForeignKey(e => new { e.OutputHash, e.OutputIndex })
                 .HasPrincipalKey(e => new { e.TransactionHash, e.Index })
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        protected virtual void ConfigureOutput(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Output>(b =>
            {
                b.Property(e => e.TransactionHash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.Index).IsRequired();
                b.Property(e => e.Value).IsRequired();
                b.Property(e => e.Script).IsRequired().HasConversion(Converters.ScriptToBytesConverter);

                b.HasKey(e => new { e.TransactionHash, e.Index });
                b.HasOne(e => e.Transaction)
                 .WithMany(e => e.Outputs)
                 .HasForeignKey(e => e.TransactionHash)
                 .HasPrincipalKey(e => e.Hash)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }

        protected virtual void ConfigureTransaction(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>(b =>
            {
                b.Property(e => e.Hash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.BlockHash).IsRequired().HasConversion(Converters.UInt256ToBytesConverter);
                b.Property(e => e.Version).IsRequired();
                b.Property(e => e.LockTime).IsRequired();

                b.HasKey(e => new { e.Hash, e.BlockHash });
                b.HasIndex(e => e.BlockHash);
                b.HasOne(e => e.Block)
                 .WithMany(e => e.Transactions)
                 .HasForeignKey(e => e.BlockHash)
                 .HasPrincipalKey(e => e.Hash)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }

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
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureBlock(modelBuilder);
            ConfigureInput(modelBuilder);
            ConfigureOutput(modelBuilder);
            ConfigureTransaction(modelBuilder);
            ConfigureWebApiCallback(modelBuilder);
        }
    }
}
