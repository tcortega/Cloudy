using Cloudy.Parallelization;

namespace Cloudy.Models.Checker;

public class CheckerInfo
{
    private long _hits;
    private long _custom;
    private long _retries;
    private long _errors;
    private long _invalid;

    public CheckerInfo(long dataSize, long skipped)
    {
        DataSize = dataSize;
        Skipped = skipped;
    }

    internal IParallelizerStats? ParallelizerStats { get; set; }
    public CheckerStatus Status { get; internal set; } = CheckerStatus.Idle;
    
    public long DataSize { get; }
    
    public long Skipped { get; }
    public long Hits => _hits;
    public long Custom => _custom;
    public long Retries => _retries;
    public long Errors => _errors;
    public long Invalid => _invalid;
    public long Checked => Hits + Custom + Errors + Invalid + Skipped;
    public int CPM => ParallelizerStats?.CPM ?? 0;
    public DateTime StartTime => ParallelizerStats?.StartTime ?? DateTime.MinValue;
    public DateTime ETA => ParallelizerStats?.ETA ?? DateTime.MinValue;
    public DateTime? EndTime => ParallelizerStats?.EndTime;
    public TimeSpan? Elapsed => ParallelizerStats?.Elapsed;
    public TimeSpan? Remaining => ParallelizerStats?.Remaining;
    public float Progress => ParallelizerStats?.Progress ?? 0;

    internal void IncrementHit()
        => Interlocked.Increment(ref _hits);
    internal void IncrementCustom()
        => Interlocked.Increment(ref _custom);
    internal void IncrementRetry()
        => Interlocked.Increment(ref _retries);
    internal void IncrementError()
        => Interlocked.Increment(ref _errors);
    internal void IncrementInvalid()
        => Interlocked.Increment(ref _invalid);
}