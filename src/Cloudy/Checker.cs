using Cloudy.Http;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;
using Cloudy.Models.Data.DataPools;
using Cloudy.Utilities;
using RuriLib.Parallelization;
using RuriLib.Parallelization.Models;

namespace Cloudy;

public class Checker<TInput> where TInput : ICredential
{
    private readonly CheckerInfo _info;
    private readonly CheckerSettings _settings;
    private readonly DataPool _dataPool;
    private readonly Pool<CloudyHttpClient> _httpClientPool;
    private readonly Func<string, IDataParseResult<TInput>> _dataParser;
    private readonly Func<BotData<TInput>, CloudyHttpClient, Task<CheckResult>> _checkerFunc;
    private readonly OutputHandler<TInput> _outputHandler;
    private readonly object _locker = new();

    internal Checker(CheckerSettings settings, DataPool dataPool, Pool<CloudyHttpClient> httpClientPool,
        OutputHandler<TInput> outputHandler, Func<string, IDataParseResult<TInput>> dataParser,
        Func<BotData<TInput>, CloudyHttpClient, Task<CheckResult>> checkerFunc)
    {
        _info = new CheckerInfo();
        _settings = settings;
        _dataPool = dataPool;
        _httpClientPool = httpClientPool;
        _dataParser = dataParser;
        _outputHandler = outputHandler;
        _checkerFunc = checkerFunc;
    }

    public async Task StartAsync()
    {
        if (_info.Status != CheckerStatus.Idle)
        {
            throw new InvalidOperationException("Checker is already running.");
        }

        var workItems = _dataPool.DataList.Select(line => new BotData<TInput>(_dataParser(line)));
        var parallelizer = ParallelizerFactory<BotData<TInput>, CheckResult>.Create(ParallelizerType.TaskBased,
            workItems, WorkFunction, _settings.Threads, maxDegreeOfParallelism: _settings.MaxThreads,
            totalAmount: _dataPool.Size);

        parallelizer.CPMLimit = _settings.MaxCpm;
        parallelizer.NewResult += ProcessParallelizerResult;
        await parallelizer.Start();
        await parallelizer.WaitCompletion();
    }

    private void ProcessParallelizerResult(object? sender,
        ResultDetails<BotData<TInput>, CheckResult> parallelizerResult)
    {
        var checkResult = parallelizerResult.Result;

        switch (checkResult.Status)
        {
            case CheckResultStatus.Hit:
                Interlocked.Increment(ref _info.Hits);
                break;
            case CheckResultStatus.Custom:
                Interlocked.Increment(ref _info.Custom);
                break;
            case CheckResultStatus.Invalid:
                Interlocked.Increment(ref _info.Invalid);
                break;
            case CheckResultStatus.Error:
                Interlocked.Increment(ref _info.Invalid);
                break;
            case CheckResultStatus.Retry:
            default:
                throw new ArgumentOutOfRangeException(nameof(CheckResultStatus), "This enum shouldn't be here at all.");
        }

        lock (_locker)
        {
            _outputHandler.OutputFunc(parallelizerResult.Item, checkResult);
        }

        // ReSharper disable once InconsistentlySynchronizedField
        _outputHandler.AfterOutputAsync?.Invoke(parallelizerResult.Item, checkResult);
    }

    private async Task<CheckResult> WorkFunction(BotData<TInput> botData, CancellationToken token)
    {
        if (!botData.Input.IsValid) return CheckResult.FromInvalid();

        botData.CancellationToken = token;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var httpClient = _settings.UseProxies ? _httpClientPool.Borrow(token) : BotData<TInput>.UnproxifiedClient;
            CheckResult result;
            try
            {
                result = await _checkerFunc(botData, httpClient).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                botData.Attempts++;
                if (botData.Attempts > _settings.MaxAttempts)
                {
                    result = CheckResult.FromException(e);
                    return result;
                }

                continue;
            }
            finally
            {
                _httpClientPool.Return(httpClient);
            }

            if (result.Status == CheckResultStatus.Retry)
            {
                Interlocked.Increment(ref _info.Retries);
                continue;
            }

            token.ThrowIfCancellationRequested();
            return result;
        }
    }
}