namespace Cloudy.Models.Data;

public interface IDataParseResult<out TInput>
{
    public bool IsValid { get; }
    public TInput Value { get; }
}