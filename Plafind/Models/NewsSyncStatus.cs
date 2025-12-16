namespace Plafind.Models
{
    /// <summary>
    /// Haber senkronizasyon durumu için model
    /// </summary>
    public class NewsSyncStatus
    {
        public DateTime LastSyncTime { get; set; }
        public int LastSyncItemCount { get; set; }
        public bool IsSuccess { get; set; }
        public string? LastErrorMessage { get; set; }
        public TimeSpan? LastSyncDuration { get; set; }
        public int TotalSyncedItems { get; set; }
        public bool IsBackgroundServiceRunning { get; set; }
        public DateTime? ServiceStartTime { get; set; }
        public int ServiceTotalRuns { get; set; }
        public DateTime? ServiceLastRunTime { get; set; }
        public TimeSpan? TimeSinceLastSync { get; set; }
    }
}

