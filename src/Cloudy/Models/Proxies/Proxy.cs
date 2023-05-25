using System.Net;

namespace Cloudy.Models.Proxies;

public class Proxy
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public ProxyType Type { get; set; }

    public Proxy(string host, int port, ProxyType type = ProxyType.Http, string username = "", string password = "")
    {
        Host = host;
        Port = port;
        Type = type;
        Username = username;
        Password = password;
    }

    public NetworkCredential? Credentials => !string.IsNullOrEmpty(Username) ? new NetworkCredential(Username, Password) : null;

    public static bool TryParse(string proxyString, out Proxy? proxy, ProxyType defaultType = ProxyType.Http)
    {
        try
        {
            proxy = Parse(proxyString, defaultType);
            return true;
        }
        catch
        {
            proxy = default;
            return false;
        }
    }
    
    public static Proxy Parse(string proxyString, ProxyType proxyType = ProxyType.Http)
    {
        if (string.IsNullOrWhiteSpace(proxyString))
            throw new ArgumentNullException(nameof(proxyString));

        var proxy = new Proxy(string.Empty, 0, proxyType);

        if (!proxyString.Contains(':'))
            throw new FormatException("Expected at least 2 colon-separated fields");

        var fields = proxyString.Split(':');
        proxy.Host = fields[0];

        if (!int.TryParse(fields[1], out var port))
            throw new FormatException("The proxy port must be an integer");

        proxy.Port = port;

        if (fields.Length == 3)
            throw new FormatException("Expected 4 colon-separated fields, got 3");

        // Set the other two if they are present
        if (fields.Length <= 2) return proxy;

        proxy.Username = fields[2];
        proxy.Password = fields[3];

        return proxy;
    }

    public override string ToString()
        => $"{Type.ToString().ToLower()}://{Host}:{Port}";
}