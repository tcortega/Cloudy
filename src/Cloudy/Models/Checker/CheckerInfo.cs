namespace Cloudy.Models.Checker;

public class CheckerInfo
{
    public CheckerStatus Status { get; internal set; }
    internal long Hits = 0;
    internal long Custom = 0;
    internal long Retries = 0;
    internal long Errors = 0;
    internal long Invalid = 0;
}