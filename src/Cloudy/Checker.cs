using Cloudy.Http;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;
using Cloudy.Models.Data.DataPools;
using Cloudy.Parallelization;
using Cloudy.Parallelization.Models;
using Cloudy.Utilities;

namespace Cloudy;

/// <summary>
/// The main class responsible for managing the checking process.
/// </summary>
/// <typeparam name="TInput">The type of the credential used by the Checker.</typeparam>
public class Checker<TInput> where TInput : ICredential
{
    private readonly CheckerSettings _settings;
    private readonly DataPool _dataPool;
    private readonly Pool<CloudyHttpClient> _httpClientPool;
    private readonly DataParser<TInput> _dataParser;
    private readonly CheckerDelegate<TInput> _checkerFunc;
    private readonly OutputThread<TInput> _outputThread;
    private Parallelizer<BotData<TInput>, CheckResult> _parallelizer = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Checker{TInput}"/> class.
    /// </summary>
    /// <param name="settings">The <see cref="CheckerSettings"/> to configure the Checker.</param>
    /// <param name="dataPool">The <see cref="DataPool"/> used by the Checker.</param>
    /// <param name="httpClientPool">HTTP client pool used by the Checker.</param>
    /// <param name="outputThread">The <see cref="OutputThread{TInput}"/> used by the Checker.</param>
    /// <param name="dataParser">The <see cref="DataParser{TInput}"/> used by the Checker.</param>
    /// <param name="checkerFunc">The <see cref="CheckerDelegate{TInput}"/> used by the Checker.</param>
    internal Checker(CheckerSettings settings, DataPool dataPool, Pool<CloudyHttpClient> httpClientPool,
        OutputThread<TInput> outputThread, DataParser<TInput> dataParser, CheckerDelegate<TInput> checkerFunc)
    {
        _settings = settings;
        _dataPool = dataPool;
        _httpClientPool = httpClientPool;
        _dataParser = dataParser;
        _outputThread = outputThread;
        _checkerFunc = checkerFunc;
        Info = new(dataPool.Size, settings.Skip);
    }
    
    public CheckerInfo Info { get; }

    /// <summary>
    /// Starts the Checker without awaiting for completion
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the Checker is already running.</exception>
    public async Task Start()
    {
        if (Info.Status != CheckerStatus.Idle)
        {
            throw new InvalidOperationException("The checking process has already initiated.");
        }
        
        Info.Status = CheckerStatus.Starting;
        _ = _outputThread.StartAsync();

        var workItems = _dataPool.DataList.Select(line => new BotData<TInput>(_dataParser(line)));
        _parallelizer = ParallelizerFactory<BotData<TInput>, CheckResult>.Create(ParallelizerType.TaskBased,
            workItems, WorkFunction, _settings.Threads, maxDegreeOfParallelism: _settings.MaxThreads,
            totalAmount: _dataPool.Size, skip: _settings.Skip);

        Info.ParallelizerStats = _parallelizer;
        _parallelizer.CPMLimit = _settings.MaxCpm;
        _parallelizer.NewResult += ProcessParallelizerResult;
        _parallelizer.Completed += ProcessParallelizerCompleted;
        await _parallelizer.Start();

        Info.Status = CheckerStatus.Running;
    }

    /// <summary>
    /// An awaitable handler that completes when the <see cref="ParallelizerStatus"/> is <see cref="ParallelizerStatus.Idle"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="CheckerStatus"/> is not Running.</exception>
    public async Task WaitCompletion()
    {
        ThrowOnCheckerNotRunning();
        await _parallelizer.WaitCompletion();
    }

    /// <summary>
    /// Resumes the checker if it was paused.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="CheckerStatus"/> is not Paused.</exception>
    public async Task Resume()
    {
        if (Info.Status != CheckerStatus.Paused)
            throw new InvalidOperationException("Checker is not paused.");

        Info.Status = CheckerStatus.Resuming;
        await _parallelizer.Resume();
        Info.Status = CheckerStatus.Running;
    }


    /// <summary>Pauses the execution (waits until the ongoing operations are completed).</summary>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="CheckerStatus"/> is not Running.</exception>
    public async Task Pause()
    {
        ThrowOnCheckerNotRunning();
        Info.Status = CheckerStatus.Pausing;
        await _parallelizer.Pause();
        Info.Status = CheckerStatus.Paused;
    }

    /// <summary>
    /// Stops the execution (waits for the current items to finish).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="CheckerStatus"/> is not Running.</exception>
    public async Task Stop()
    {
        ThrowOnCheckerNotRunning();

        Info.Status = CheckerStatus.Stopping;
        await _parallelizer.Stop();
        _settings.CancellationTokenSource.Cancel(); // Makes sure that the OutputThread will be stopped
    }

    /// <summary>
    /// Aborts the execution without waiting for the current work to finish.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="CheckerStatus"/> is not Running.</exception>
    public async Task Abort()
    {
        ThrowOnCheckerNotRunning();

        await _parallelizer.Abort();
        _settings.CancellationTokenSource.Cancel(); // Makes sure that the OutputThread will be stopped
    }

    private async Task<CheckResult> WorkFunction(BotData<TInput> botData, CancellationToken token)
    {
        if (!botData.ParseResult.IsValid) return CheckResult.FromInvalid();

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
                Info.IncrementRetry();
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
                botData.Attempts++;
                Info.IncrementRetry();
                if (botData.Attempts > _settings.MaxAttempts)
                {
                    return result;
                }
            }

            token.ThrowIfCancellationRequested();
            return result;
        }
    }

    private void ProcessParallelizerResult(object? sender, ResultDetails<BotData<TInput>, CheckResult> parallelizerResult)
    {
        var checkResult = parallelizerResult.Result;

        switch (checkResult.Status)
        {
            case CheckResultStatus.Hit:
                Info.IncrementHit();
                break;
            case CheckResultStatus.Custom:
                Info.IncrementCustom();
                break;
            case CheckResultStatus.Invalid:
                Info.IncrementInvalid();
                break;
            case CheckResultStatus.Error:
                Info.IncrementError();
                break;
            case CheckResultStatus.Retry:
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unexpected value for CheckResultStatus: {checkResult.Status}");
        }

        _outputThread.AddItem(parallelizerResult.Item, checkResult);
    }

    private void ProcessParallelizerCompleted(object? sender, EventArgs e)
    {
        Info.Status = CheckerStatus.Completed;
        _settings.CancellationTokenSource.Cancel();
        _parallelizer.NewResult -= ProcessParallelizerResult;
        _parallelizer.Completed -= ProcessParallelizerCompleted;
    }

    private void ThrowOnCheckerNotRunning()
    {
        if (Info.Status != CheckerStatus.Running)
        {
            throw new InvalidOperationException("Checker is not running.");
        }
    }
}