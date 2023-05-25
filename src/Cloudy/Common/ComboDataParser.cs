using Cloudy.Models.Data;

namespace Cloudy.Common;

public static partial class DataParser
{
    public static string Separator { get; set; } = ":";
    public static DataParseResult<ComboCredential> ParseCombo(string line)
    {
        if (!line.Contains(Separator))
            DataParseResult<ComboCredential>.FromInvalid(null!);
        
        var parts = line.Split(Separator);

        return DataParseResult<ComboCredential>.FromValid(new(parts[0], parts[1], line));
    }
}