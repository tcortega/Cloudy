namespace Cloudy.Models.Checker;

public enum CheckResultStatus
{
    Invalid,
    Hit,
    Custom,
    Retry,
    Error
}

public static class EnumExtensions
{
    public static string ToStringFast(this CheckResultStatus status)
        => status switch
        {
            CheckResultStatus.Invalid => "Invalid",
            CheckResultStatus.Hit => "Hit",
            CheckResultStatus.Custom => "Custom",
            CheckResultStatus.Retry => "Retry",
            CheckResultStatus.Error => "Error",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}