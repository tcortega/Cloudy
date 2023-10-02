using Cloudy.Models.Checker;
using Cloudy.Models.Data;

namespace Cloudy.Test.Data;

public sealed record SampleCredential(Guid Id, CheckResultStatus ExpectedStatus) : ICredential
{
    public string Raw => Id.ToString();
    
    public static DataParser<SampleCredential> Parser => line =>
    {
        if (!line.Contains(':'))
            return DataParseResult<SampleCredential>.FromInvalid(null!);
        
        var parts = line.Split(':');
        if (!Guid.TryParse(parts[0], out var guid))
            return DataParseResult<SampleCredential>.FromInvalid(null!);
        
        if (!Enum.TryParse<CheckResultStatus>(parts[1], out var enumValue))
            return DataParseResult<SampleCredential>.FromInvalid(null!);
        
        return DataParseResult<SampleCredential>.FromValid(new(guid, enumValue));
    };
}