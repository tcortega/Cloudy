namespace Cloudy.Parallelization;

public interface IParallelizerStats
{
    int MaxDegreeOfParallelism { get; }
    ParallelizerStatus Status { get; }
    float Progress { get; }
    int CPM { get; }
    int CPMLimit { get; set; }
    DateTime StartTime { get; }
    DateTime? EndTime { get; }
    DateTime ETA { get; }
    TimeSpan Elapsed { get; }
    TimeSpan Remaining { get; }
}