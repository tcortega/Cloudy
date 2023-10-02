namespace Cloudy.Models.Checker;

public enum CheckerStatus
{
    Idle,
    Starting,
    Completed,
    Resuming,
    Pausing,
    Running,
    Paused,
    Stopping
}