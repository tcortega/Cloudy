namespace Cloudy.Models.Checker;

public class CheckResult
{
    public CheckResultStatus Status { get; }
    public Dictionary<string, object>? Captures { get; }
    public Exception? Exception { get; }
    
    protected CheckResult(Dictionary<string, object>? captures, CheckResultStatus status)
    {
        Captures = captures;
        Status = status;
    }

    protected CheckResult(CheckResultStatus status, Exception exception)
    {
        Status = status;
        Exception = exception;
    }
    
    public static CheckResult FromHit(Dictionary<string, object>? captures = null)
    {
        return new(captures, CheckResultStatus.Hit);
    }
    
    public static CheckResult FromCustom(Dictionary<string, object>? captures = null)
    {
        return new(captures, CheckResultStatus.Custom);
    }
    
    public static CheckResult FromInvalid()
    {
        return new(null, CheckResultStatus.Invalid);
    }

    public static CheckResult FromException(Exception exception)
    {
        return new(CheckResultStatus.Error, exception);
    }
}