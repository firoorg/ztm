using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Testing
{
    public class TestMainDatabase : MainDatabase
    {
        public TestMainDatabase(DbContextOptions<MainDatabase> options) : base(options)
        {
        }

        protected override void ConfigureBlock(EntityTypeBuilder<Block> builder)
        {
            base.ConfigureBlock(builder);

            builder.Property(e => e.Hash).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.MerkleRoot).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.MtpHashValue).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.Reserved1).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.Reserved2).HasConversion(Converters.UInt256ToBytesConverter);
        }

        protected override void ConfigureBlockTransaction(EntityTypeBuilder<BlockTransaction> builder)
        {
            base.ConfigureBlockTransaction(builder);

            builder.Property(e => e.BlockHash).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.TransactionHash).HasConversion(Converters.UInt256ToBytesConverter);
        }

        protected override void ConfigureInput(EntityTypeBuilder<Input> builder)
        {
            base.ConfigureInput(builder);

            builder.Property(e => e.TransactionHash).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.OutputHash).HasConversion(Converters.UInt256ToBytesConverter);
        }

        protected override void ConfigureOutput(EntityTypeBuilder<Output> builder)
        {
            base.ConfigureOutput(builder);

            builder.Property(e => e.TransactionHash).HasConversion(Converters.UInt256ToBytesConverter);
        }

        protected override void ConfigureReceivingAddress(EntityTypeBuilder<ReceivingAddress> builder)
        {
            base.ConfigureReceivingAddress(builder);

            builder.Property(e => e.Id).HasConversion<string>();
        }

        protected override void ConfigureReceivingAddressReservation(
            EntityTypeBuilder<ReceivingAddressReservation> builder)
        {
            base.ConfigureReceivingAddressReservation(builder);

            builder.Property(e => e.Id).HasConversion<string>();
            builder.Property(e => e.AddressId).HasConversion<string>();
        }

        protected override void ConfigureTokenBalanceWatcherRule(EntityTypeBuilder<TokenBalanceWatcherRule> builder)
        {
            base.ConfigureTokenBalanceWatcherRule(builder);

            builder.Property(e => e.Id).HasConversion<string>();
            builder.Property(e => e.CallbackId).HasConversion<string>();
        }

        protected override void ConfigureTokenBalanceWatcherWatch(EntityTypeBuilder<TokenBalanceWatcherWatch> builder)
        {
            base.ConfigureTokenBalanceWatcherWatch(builder);

            builder.Property(e => e.Id).HasConversion<string>();
            builder.Property(e => e.RuleId).HasConversion<string>();
            builder.Property(e => e.BlockId).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.TransactionId).HasConversion(Converters.UInt256ToBytesConverter);
        }

        protected override void ConfigureTransaction(EntityTypeBuilder<Transaction> builder)
        {
            base.ConfigureTransaction(builder);

            builder.Property(e => e.Hash).HasConversion(Converters.UInt256ToBytesConverter);
        }

        protected override void ConfigureTransactionConfirmationWatcherRule(EntityTypeBuilder<TransactionConfirmationWatcherRule> builder)
        {
            base.ConfigureTransactionConfirmationWatcherRule(builder);

            builder.Property(e => e.Id).HasConversion<string>();
            builder.Property(e => e.CallbackId).HasConversion<string>();
            builder.Property(e => e.TransactionHash).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.CurrentWatchId).HasConversion<string>();
        }

        protected override void ConfirgureTransactionConfirmationWatcherWatch(EntityTypeBuilder<TransactionConfirmationWatcherWatch> builder)
        {
            base.ConfirgureTransactionConfirmationWatcherWatch(builder);

            builder.Property(e => e.Id).HasConversion<string>();
            builder.Property(e => e.RuleId).HasConversion<string>();
            builder.Property(e => e.StartBlockHash).HasConversion(Converters.UInt256ToBytesConverter);
            builder.Property(e => e.TransactionHash).HasConversion(Converters.UInt256ToBytesConverter);
        }

        protected override void ConfigureWebApiCallback(EntityTypeBuilder<WebApiCallback> builder)
        {
            base.ConfigureWebApiCallback(builder);

            builder.Property(e => e.Id).HasConversion<string>();
            builder.Property(e => e.RegisteredIp).HasConversion(Converters.IPAddressToStringConverter);
            builder.Property(e => e.Url).HasConversion(Converters.UriToStringConverter);
        }
    }
}
