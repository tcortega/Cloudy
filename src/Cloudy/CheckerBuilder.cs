using System.Security.Authentication;
using Cloudy.Http;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;
using Cloudy.Models.Data.DataPools;
using Cloudy.Models.Proxies;
using Cloudy.Utilities;

namespace Cloudy;

/// <summary>
/// A builder class for creating an instance of a Checker.
/// </summary>
/// <typeparam name="TInput">The type of credential the Checker will be working with.</typeparam>
public class CheckerBuilder<TInput> where TInput : ICredential
{
    private readonly CheckerSettings _settings;
    private readonly DataPool _dataPool;
    private readonly DataParser<TInput> _dataParser;
    private readonly CheckerDelegate<TInput> _checkerFunc;
    private readonly Dictionary<string, string> _defaultRequestHeaders = new();

    private readonly List<OutputHandler<TInput>> _outputHandlers = new();
    private List<Proxy>? _proxies;
    private ProxySettings? _proxySettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckerBuilder{TInput}"/> class.
    /// </summary>
    /// <param name="settings">The <see cref="CheckerSettings" /> for the Checker.</param>
    /// <param name="dataPool">The <see cref="DataPool" /> with the items the Checker will process.</param>
    /// <param name="dataParser">The <see cref="DataParser{TInput}"/> delegate.</param>
    /// <param name="checkerFunc">The <see cref="CheckerDelegate{TInput}"/> of the checker function.</param>
    public CheckerBuilder(CheckerSettings settings, DataPool dataPool, DataParser<TInput> dataParser, CheckerDelegate<TInput> checkerFunc)
    {
        _settings = settings;
        _dataPool = dataPool;
        _dataParser = dataParser;
        _checkerFunc = checkerFunc;
    }

    /// <summary>
    /// Configures the CheckerBuilder to use proxies.
    /// </summary>
    /// <param name="proxies">The list of proxies.</param>
    /// <param name="proxySettings">The proxy settings.</param>
    /// <returns>The current CheckerBuilder instance.</returns>
    public CheckerBuilder<TInput> WithProxies(IEnumerable<string> proxies, ProxySettings proxySettings)
    {
        _proxies = new List<Proxy>();
        foreach (var proxy in proxies)
        {
            try
            {
                _proxies.Add(Proxy.Parse(proxy, proxySettings.Protocol));
            }
            catch
            {
                // ignored
            }
        }

        _proxySettings = proxySettings;
        _settings.UseProxies = true;

        return this;
    }

    /// <summary>
    /// Configures the CheckerBuilder to use a specific SSL Protocol.
    /// </summary>
    /// <param name="protocol">The <see cref="SslProtocols"/> to use.</param>
    /// <returns>The current CheckerBuilder instance.</returns>
    public CheckerBuilder<TInput> WithSslProtocol(SslProtocols protocol)
    {
        _settings.SslProtocol = protocol;
        return this;
    }

    /// <summary>
    /// Adds a request header that will be used for every request made by every <see cref="CloudyHttpClient"/> in the pool.
    /// </summary>
    /// <param name="name">The header's name.</param>
    /// <param name="value">The header's value.</param>
    /// <returns></returns>
    public CheckerBuilder<TInput> WithDefaultRequestHeader(string name, string value)
    {
        _defaultRequestHeaders.Add(name, value);

        return this;
    }

    /// <summary>
    /// Adds multiple request headers that will be used for every request made by every <see cref="CloudyHttpClient"/> in the pool.
    /// </summary>
    /// <param name="headers">The request headers dictionary.</param>
    /// <returns></returns>
    public CheckerBuilder<TInput> WithDefaultRequestHeaders(IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            WithDefaultRequestHeader(header.Key, header.Value);
        }

        return this;
    }

    /// <summary>
    /// Sets the <see cref="OutputHandler{TInput}"/> for the Checker.
    /// </summary>
    /// <param name="outputHandler">The <see cref="OutputHandler{TInput}"/></param>
    /// <returns></returns>
    public CheckerBuilder<TInput> AddOutputHandler(OutputHandler<TInput> outputHandler)
    {
        _outputHandlers.Add(outputHandler);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="Checker{TInput}"/> instance with the set configurations.
    /// </summary>
    /// <returns>The constructed <see cref="Checker{TInput}"/> instance.</returns>
    public Checker<TInput> Build()
    {
        var httpClientPool = SetupHttpClientPool();
        var outputThread = new OutputThread<TInput>(_outputHandlers, _settings.CancellationToken, _settings.OutputDelay);

        return new Checker<TInput>(_settings, _dataPool, httpClientPool, outputThread, _dataParser, _checkerFunc);
    }

    private Pool<CloudyHttpClient> SetupHttpClientPool()
    {
        if (_proxies is { Count: 0 }) throw new InvalidOperationException("No valid proxies were loaded.");

        var unproxifiedClient = CloudyHttpClient.Build(_settings);
        BotData<TInput>.UnproxifiedClient = unproxifiedClient;

        List<CloudyHttpClient> httpClientList;
        if (_proxies is { Count: > 0 })
        {
            httpClientList = _proxies
                .Select(proxy => CloudyHttpClient.BuildFromProxy(proxy, _proxySettings!, _settings)).ToList();
        }
        else
        {
            httpClientList = new() { unproxifiedClient };
        }

        SetDefaultRequestHeaders(httpClientList);

        const int proxylessPoolSize = 1;
        var pool = Pool<CloudyHttpClient>.Create(httpClientList,
            _proxies is null ? proxylessPoolSize : _settings.MaxThreads);
        return pool;
    }

    private void SetDefaultRequestHeaders(List<CloudyHttpClient> httpClientList)
    {
        foreach (var header in _defaultRequestHeaders)
        {
            foreach (var httpClient in httpClientList)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }
}