namespace Cloudy.Models.Checker;

/// <summary>
/// Represents the result of a check.
/// </summary>
public class CheckResult
{
    /// <summary>
    /// The <see cref="CheckResultStatus"/> of the result.
    /// </summary>
    public CheckResultStatus Status { get; }

    /// <summary>
    /// The optional capture data of the check result.
    /// </summary>
    public Dictionary<string, object>? Captures { get; }

    /// <summary>
    /// The optional exception that could occur during the check.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckResult"/> class with capture data and a status.
    /// </summary>
    /// <param name="captures">The capture data of the check result.</param>
    /// <param name="status">The <see cref="CheckResultStatus"/> of the check result.</param>
    protected CheckResult(Dictionary<string, object>? captures, CheckResultStatus status)
    {
        Captures = captures;
        Status = status;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckResult"/> class with a status and an exception.
    /// </summary>
    /// <param name="status">The <see cref="CheckResultStatus"/> of the check result.</param>
    /// <param name="exception">The exception that occurred during the check.</param>
    protected CheckResult(CheckResultStatus status, Exception exception)
    {
        Status = status;
        Exception = exception;
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="CheckResult"/> class indicating a successful hit.
    /// </summary>
    /// <param name="captures">Optional capture data of the check result.</param>
    /// <returns>A successful hit <see cref="CheckResult"/>.</returns>
    public static CheckResult FromHit(Dictionary<string, object>? captures = null)
        => new(captures, CheckResultStatus.Hit);


    /// <summary>
    /// Constructs a new instance of the <see cref="CheckResult"/> class indicating a custom status.
    /// </summary>
    /// <param name="captures">Optional capture data of the check result.</param>
    /// <returns>A custom hit <see cref="CheckResult"/>.</returns>
    public static CheckResult FromCustom(Dictionary<string, object>? captures = null)
        => new(captures, CheckResultStatus.Custom);


    /// <summary>
    /// Constructs a new instance of the <see cref="CheckResult"/> class indicating an invalid status.
    /// </summary>
    /// <returns>An invalid status <see cref="CheckResult"/>.</returns>
    public static CheckResult FromInvalid()
        => new(null, CheckResultStatus.Invalid);


    /// <summary>
    /// Constructs a new instance of the <see cref="CheckResult"/> class indicating an error, with an exception.
    /// </summary>
    /// <param name="exception">The exception that occurred during the check.</param>
    /// <returns>An error status <see cref="CheckResult"/>.</returns>
    public static CheckResult FromException(Exception exception)
        => new(CheckResultStatus.Error, exception);
    
    /// <summary>
    /// Constructs a new instance of the <see cref="CheckResult"/> class indicating a retry status.
    /// </summary>
    /// <returns>An error status <see cref="CheckResult"/>.</returns>
    public static CheckResult FromRetry()
        => new(null, CheckResultStatus.Retry);
}