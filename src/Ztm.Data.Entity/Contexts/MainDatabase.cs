using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Contexts
{
    /// <summary>
    /// Represents the application main database.
    /// </summary>
    /// <remark>
    /// Adds all configurations explicitly and ignores EF conventions.
    /// </remark>
    public class MainDatabase : DbContext
    {
        public MainDatabase(DbContextOptions<MainDatabase> options) : base(options)
        {
        }

        public DbSet<Block> Blocks { get; set; }

        public DbSet<BlockTransaction> BlockTransactions { get; set; }

        public DbSet<Input> Inputs { get; set; }

        public DbSet<Output> Outputs { get; set; }

        public DbSet<ReceivingAddress> ReceivingAddresses { get; set; }

        public DbSet<ReceivingAddressReservation> ReceivingAddressReservations { get; set; }

        public DbSet<TransactionConfirmationWatcherRule> TransactionConfirmationWatcherRules { get; set; }

        public DbSet<TransactionConfirmationWatcherWatch> TransactionConfirmationWatcherWatches { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<WebApiCallback> WebApiCallbacks { get; set; }

        public DbSet<WebApiCallbackHistory> WebApiCallbackHistories { get; set; }

        protected virtual void ConfigureBlock(EntityTypeBuilder<Block> builder)
        {
            builder.Property(e => e.Height).IsRequired().ValueGeneratedNever();
            builder.Property(e => e.Hash).IsRequired();
            builder.Property(e => e.Version).IsRequired();
            builder.Property(e => e.Bits).IsRequired().HasConversion(Converters.TargetToInt64);
            builder.Property(e => e.Nonce).IsRequired();
            builder.Property(e => e.Time).IsRequired();
            builder.Property(e => e.MerkleRoot).IsRequired();
            builder.Property(e => e.MtpVersion);
            builder.Property(e => e.MtpHashValue);
            builder.Property(e => e.Reserved1);
            builder.Property(e => e.Reserved2);

            builder.HasKey(e => e.Height);
            builder.HasAlternateKey(e => e.Hash);
        }

        protected virtual void ConfigureBlockTransaction(EntityTypeBuilder<BlockTransaction> builder)
        {
            builder.Property(e => e.BlockHash).IsRequired();
            builder.Property(e => e.TransactionHash).IsRequired();
            builder.Property(e => e.Index).IsRequired();

            builder.HasKey(e => new { e.BlockHash, e.TransactionHash, e.Index });
            builder.HasIndex(e => e.TransactionHash);
            builder.HasOne(e => e.Block)
                   .WithMany(e => e.Transactions)
                   .HasForeignKey(e => e.BlockHash)
                   .HasPrincipalKey(e => e.Hash)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(e => e.Transaction)
                   .WithMany(e => e.Blocks)
                   .HasForeignKey(e => e.TransactionHash)
                   .HasPrincipalKey(e => e.Hash)
                   .OnDelete(DeleteBehavior.Restrict);
        }

        protected virtual void ConfigureInput(EntityTypeBuilder<Input> builder)
        {
            builder.Property(e => e.TransactionHash).IsRequired();
            builder.Property(e => e.Index).IsRequired();
            builder.Property(e => e.OutputHash).IsRequired();
            builder.Property(e => e.OutputIndex).IsRequired();
            builder.Property(e => e.Script).IsRequired().HasConversion(Converters.ScriptToBytesConverter);
            builder.Property(e => e.Sequence).IsRequired();

            builder.HasKey(e => new { e.TransactionHash, e.Index });
            builder.HasIndex(e => new { e.OutputHash, e.OutputIndex });
            builder.HasOne(e => e.Transaction)
                   .WithMany(e => e.Inputs)
                   .HasForeignKey(e => e.TransactionHash)
                   .HasPrincipalKey(e => e.Hash)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        protected virtual void ConfigureOutput(EntityTypeBuilder<Output> builder)
        {
            builder.Property(e => e.TransactionHash).IsRequired();
            builder.Property(e => e.Index).IsRequired();
            builder.Property(e => e.Value).IsRequired();
            builder.Property(e => e.Script).IsRequired().HasConversion(Converters.ScriptToBytesConverter);

            builder.HasKey(e => new { e.TransactionHash, e.Index });
            builder.HasOne(e => e.Transaction)
                   .WithMany(e => e.Outputs)
                   .HasForeignKey(e => e.TransactionHash)
                   .HasPrincipalKey(e => e.Hash)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        protected virtual void ConfigureReceivingAddress(EntityTypeBuilder<ReceivingAddress> builder)
        {
            builder.Property(e => e.Id).IsRequired().ValueGeneratedOnAdd();
            builder.Property(e => e.Address).IsRequired();
            builder.Property(e => e.IsLocked).IsRequired().HasDefaultValue(false);

            builder.HasKey(e => e.Id);
            builder.HasIndex(e => e.Address).IsUnique();
        }

        protected virtual void ConfigureReceivingAddressReservation(EntityTypeBuilder<ReceivingAddressReservation> builder)
        {
            builder.Property(e => e.Id).IsRequired().ValueGeneratedOnAdd();
            builder.Property(e => e.AddressId).IsRequired();
            builder.Property(e => e.LockedAt).IsRequired();
            builder.Property(e => e.ReleasedAt);

            builder.HasKey(e => e.Id);
            builder.HasIndex(e => e.AddressId);
            builder.HasOne(e => e.Address)
                   .WithMany(e => e.Reservations)
                   .HasForeignKey(e => e.AddressId)
                   .HasPrincipalKey(e => e.Id)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        protected virtual void ConfigureTransaction(EntityTypeBuilder<Transaction> builder)
        {
            builder.Property(e => e.Hash).IsRequired();
            builder.Property(e => e.Version).IsRequired();
            builder.Property(e => e.LockTime).IsRequired();

            builder.HasKey(e => e.Hash);
        }

        protected virtual void ConfigureTransactionConfirmationWatcherRule(
            EntityTypeBuilder<TransactionConfirmationWatcherRule> builder)
        {
            builder.Property(e => e.Id).IsRequired();
            builder.Property(e => e.CallbackId).IsRequired();
            builder.Property(e => e.TransactionHash).IsRequired();
            builder.Property(e => e.Status).IsRequired();
            builder.Property(e => e.Confirmation).IsRequired();
            builder.Property(e => e.OriginalWaitingTime).IsRequired();
            builder.Property(e => e.RemainingWaitingTime).IsRequired();
            builder.Property(e => e.SuccessData).IsRequired();
            builder.Property(e => e.TimeoutData).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.CurrentWatchId);

            builder.HasKey(e => e.Id);

            builder.HasIndex(e => e.CallbackId);
            builder.HasIndex(e => e.Status);

            builder.HasOne(e => e.Callback)
                   .WithMany()
                   .HasForeignKey(e => e.CallbackId)
                   .HasPrincipalKey(e => e.Id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.CurrentWatch)
                   .WithMany()
                   .HasForeignKey(e => e.CurrentWatchId)
                   .HasPrincipalKey(e => e.Id)
                   .OnDelete(DeleteBehavior.SetNull);
        }

        protected virtual void ConfirgureTransactionConfirmationWatcherWatch(
            EntityTypeBuilder<TransactionConfirmationWatcherWatch> builder)
        {
            builder.Property(e => e.Id).IsRequired().ValueGeneratedNever();
            builder.Property(e => e.RuleId).IsRequired();
            builder.Property(e => e.StartBlockHash).IsRequired();
            builder.Property(e => e.StartTime).IsRequired();
            builder.Property(e => e.TransactionHash).IsRequired();
            builder.Property(e => e.Status).IsRequired();

            builder.HasKey(e => e.Id);

            builder.HasIndex(e => e.RuleId);
            builder.HasIndex(e => e.StartBlockHash);
            builder.HasIndex(e => e.Status);

            builder.HasOne(e => e.Rule)
                   .WithMany(e => e.Watches)
                   .HasForeignKey(e => e.RuleId)
                   .HasPrincipalKey(e => e.Id)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        protected virtual void ConfigureWebApiCallback(EntityTypeBuilder<WebApiCallback> builder)
        {
            builder.Property(e => e.Id).IsRequired().ValueGeneratedNever();
            builder.Property(e => e.RegisteredIp).IsRequired();
            builder.Property(e => e.RegisteredTime).IsRequired();
            builder.Property(e => e.Completed).IsRequired();
            builder.Property(e => e.Url).IsRequired();

            builder.HasKey(e => e.Id);
        }

        protected virtual void ConfigureWebApiCallbackHistory(EntityTypeBuilder<WebApiCallbackHistory> builder)
        {
            builder.Property(e => e.Id).IsRequired().ValueGeneratedOnAdd();
            builder.Property(e => e.CallbackId).IsRequired();
            builder.Property(e => e.Status).IsRequired();
            builder.Property(e => e.InvokedTime).IsRequired();
            builder.Property(e => e.Data).IsRequired();

            builder.HasKey(e => e.Id);
            builder.HasIndex(e => e.CallbackId);
            builder.HasOne(e => e.Callback)
                   .WithMany(e => e.InvocationHistories)
                   .HasForeignKey(e => e.CallbackId)
                   .HasPrincipalKey(e => e.Id)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Block>(ConfigureBlock);
            modelBuilder.Entity<BlockTransaction>(ConfigureBlockTransaction);
            modelBuilder.Entity<Input>(ConfigureInput);
            modelBuilder.Entity<Output>(ConfigureOutput);
            modelBuilder.Entity<ReceivingAddress>(ConfigureReceivingAddress);
            modelBuilder.Entity<ReceivingAddressReservation>(ConfigureReceivingAddressReservation);
            modelBuilder.Entity<Transaction>(ConfigureTransaction);
            modelBuilder.Entity<TransactionConfirmationWatcherRule>(ConfigureTransactionConfirmationWatcherRule);
            modelBuilder.Entity<TransactionConfirmationWatcherWatch>(ConfirgureTransactionConfirmationWatcherWatch);
            modelBuilder.Entity<WebApiCallback>(ConfigureWebApiCallback);
            modelBuilder.Entity<WebApiCallbackHistory>(ConfigureWebApiCallbackHistory);
        }
    }
}
