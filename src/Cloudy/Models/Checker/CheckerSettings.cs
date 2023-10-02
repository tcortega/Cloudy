using System.Security.Authentication;
using Cloudy.Http;
using Cloudy.Models.Data.DataPools;
using Cloudy.Parallelization;

namespace Cloudy.Models.Checker;

/// <summary>
/// The settings for the Checker.
/// </summary>
/// <param name="Threads">The amount of inputs that will be processed concurrently.</param>
/// <param name="MaxThreads">The maximum amount of inputs that can be processed concurrently. Defaults to 200.</param>
/// <param name="MaxCpm">The maximum checks per minute the checker can make. Used for throttling the <see cref="Parallelizer{TInput,TOutput}"/>. Defaults to 0 (unlimited)</param>
/// <param name="UseCookies">Whether or not to use cookies in the <see cref="CloudyHttpClient"/>. Defaults to false.</param>
/// <param name="AllowAutoRedirect">Whether or not to follow redirections automatically during http requests. Defaults to true.</param>
/// <param name="MaxAutoRedirections">The maximum amount of redirectionts that we will follow automatically. Defaults to 10.</param>
/// <param name="MaxAttempts">The maximum amount of attempts that will be made to check an individual credential. Defaults to 5.</param>
/// <param name="OutputDelay">The output delay used in the <see cref="OutputThread{TInput}"/>.</param>
public record CheckerSettings(int Threads, int MaxThreads = 200, int MaxCpm = 0, bool UseCookies = false, bool AllowAutoRedirect = true, int MaxAutoRedirections = 10,
    int MaxAttempts = 5, int MaxRetries = 5, TimeSpan? OutputDelay = null)
{
    public bool UseProxies { get; internal set; }
    public SslProtocols? SslProtocol { get; internal set; }
    public CancellationTokenSource CancellationTokenSource { get; } = new();
    public CancellationToken CancellationToken => CancellationTokenSource.Token;

    /// <summary>
    /// The amount of items to skip from the provided <see cref="DataPool"/>.
    /// </summary>
    public int Skip { get; set;  } = 0;
}