namespace Cloudy.Models.Proxies;

public class ProxySettings
{
    public ProxySettings(ProxyType protocol, bool rotating = false)
    {
        Protocol = protocol;
        Rotating = rotating;
    }

    public ProxyType Protocol { get; }

    /// <summary>
    /// This is required for rotating proxies, otherwise the IP won't rotate because of the socket connection being kept open
    /// </summary>
    public bool Rotating { get; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}