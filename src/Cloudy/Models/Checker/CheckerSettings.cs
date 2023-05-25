using System.Security.Authentication;

namespace Cloudy.Models.Checker;

public record CheckerSettings(int Threads, int MaxThreads = 200, int MaxCpm = 0, bool UseCookies = false, bool AllowAutoRedirect = true, int MaxAutoRedirections = 10,
    int MaxAttempts = 5)
{
    public bool UseProxies { get; internal set; }
    public SslProtocols? SslProtocol { get; internal set; }
}