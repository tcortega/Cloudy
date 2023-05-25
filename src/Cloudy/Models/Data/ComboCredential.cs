namespace Cloudy.Models.Data;

public class ComboCredential : ICredential
{
    public string Username { get; }
    public string Password { get; }
    public string Raw { get; }

    public ComboCredential(string username, string password, string raw)
    {
        Username = username;
        Password = password;
        Raw = raw;
    }

    public override string ToString() => Raw;
}