using System.Security.Authentication;
using Cloudy.Common;
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
    private readonly Func<string, IDataParseResult<TInput>> _dataParser;
    private readonly Func<BotData<TInput>, CloudyHttpClient, Task<CheckResult>> _checkerFunc;
    private readonly Dictionary<string, string> _defaultRequestHeaders = new();

    private OutputHandler<TInput> _outputHandler = new FileOutputHandler<TInput>();
    private List<Proxy>? _proxies;
    private ProxySettings? _proxySettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckerBuilder{TInput}"/> class.
    /// </summary>
    /// <param name="settings">The settings for the Checker.</param>
    /// <param name="dataPool">The data pool for the Checker.</param>
    /// <param name="dataParser">The data parser for the Checker.</param>
    /// <param name="checkerFunc">The checking function for the Checker.</param>
    public CheckerBuilder(CheckerSettings settings, DataPool dataPool,
        Func<string, IDataParseResult<TInput>> dataParser,
        Func<BotData<TInput>, CloudyHttpClient, Task<CheckResult>> checkerFunc)
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
    /// <param name="protocol">The SSL protocol to use.</param>
    /// <returns>The current CheckerBuilder instance.</returns>
    public CheckerBuilder<TInput> WithSslProtocol(SslProtocols protocol)
    {
        _settings.SslProtocol = protocol;
        return this;
    }

    public CheckerBuilder<TInput> WithDefaultRequestHeader(string name, string value)
    {
        _defaultRequestHeaders.Add(name, value);

        return this;
    }

    public CheckerBuilder<TInput> WithDefaultRequestHeaders(IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            WithDefaultRequestHeader(header.Key, header.Value);
        }

        return this;
    }

    public CheckerBuilder<TInput> WithOutputHandler(OutputHandler<TInput> outputHandler)
    {
        _outputHandler = outputHandler;
        return this;
    }

    public Checker<TInput> Build()
    {
        SetupMiscellaneous();
        var httpClientPool = SetupHttpClientPool();

        return new Checker<TInput>(_settings, _dataPool, httpClientPool, _outputHandler, _dataParser, _checkerFunc);
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
            httpClientList = new List<CloudyHttpClient> { unproxifiedClient };
        }
        
        SetDefaultRequestHeaders(httpClientList);

        const int proxylessPoolSize = 1;
        var pool = Pool<CloudyHttpClient>.Create(httpClientList, _proxies is null ? proxylessPoolSize : _settings.MaxThreads);
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

    private void SetupMiscellaneous()
    {
        Directory.CreateDirectory(_outputHandler.Directory);
    }
}