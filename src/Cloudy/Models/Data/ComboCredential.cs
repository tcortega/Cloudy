namespace Cloudy.Models.Data;

/// <summary>
/// Represents a combination of username and password credential.
/// Implements the <see cref="ICredential"/> interface.
/// </summary>
public record ComboCredential(string Username, string Password, string Raw) : ICredential
{
    /// <summary>
    /// Returns the raw string representation of the credential.
    /// </summary>
    /// <returns>The raw string representation of the credential.</returns>
    public override string ToString() => Raw;

    /// <summary>
    /// The separator used to separate the username and password.
    /// </summary>
    public static string Separator { get; set; } = ":";

    /// <summary>
    /// The <see cref="ComboCredential"/> type's data parser.
    /// </summary>
    public static DataParser<ComboCredential> Parser => line =>
    {
        if (!line.Contains(Separator))
            DataParseResult<ComboCredential>.FromInvalid(null!);

        var parts = line.Split(Separator);

        return DataParseResult<ComboCredential>.FromValid(new(parts[0], parts[1], line));
    };
}
