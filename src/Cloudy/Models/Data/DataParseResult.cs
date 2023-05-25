namespace Cloudy.Models.Data;

public class DataParseResult<TInput> : IDataParseResult<TInput> where TInput : ICredential
{
    public TInput Value { get; }
    public bool IsValid { get; }

    protected DataParseResult(TInput value, bool isValid)
    {
        Value = value;
        IsValid = isValid;
    }

    public static DataParseResult<TInput> FromValid(TInput value)
        => new(value, true);
    
    public static DataParseResult<TInput> FromInvalid(TInput value)
        => new(value, false);
}