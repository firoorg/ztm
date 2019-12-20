using System;
using System.Collections.ObjectModel;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class TransactionConfirmationWatcherRule
    {
        public Guid Id { get; set; }
        public Guid CallbackId { get; set; }
        public uint256 TransactionHash { get; set; }
        public int Status { get; set; }
        public int Confirmation { get; set; }
        public TimeSpan OriginalWaitingTime { get; set; }
        public TimeSpan RemainingWaitingTime { get; set; }
        public string SuccessData { get; set; }
        public string TimeoutData { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CurrentWatchId { get; set; }

        public WebApiCallback Callback { get; set; }
        public TransactionConfirmationWatcherWatch CurrentWatch { get; set; }
        public Collection<TransactionConfirmationWatcherWatch> Watches { get; set; }
    }
}