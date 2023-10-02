using System.Collections.Concurrent;

namespace Cloudy.Parallelization;

/// <summary>
/// Parallelizer that expoits batches of multiple tasks and the WaitAll function.
/// </summary>
public class TaskBasedParallelizer<TInput, TOutput> : Parallelizer<TInput, TOutput>
{
    private int BatchSize => MaxDegreeOfParallelism * 2;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<TInput> _queue = new();
    private int _savedDop;
    private bool _dopDecreaseRequested;

    /// <inheritdoc/>
    public TaskBasedParallelizer(IEnumerable<TInput> workItems, Func<TInput, CancellationToken, Task<TOutput>> workFunction,
        int degreeOfParallelism, long totalAmount, int skip = 0, int maxDegreeOfParallelism = 200)
        : base(workItems, workFunction, degreeOfParallelism, totalAmount, skip, maxDegreeOfParallelism)
    {
        _semaphore = new(degreeOfParallelism, MaxDegreeOfParallelism);
    }

    /// <inheritdoc/>
    public override async Task Start()
    {
        CheckDisposed();
            
        await base.Start().ConfigureAwait(false);
        Stopwatch.Restart();
        Status = ParallelizerStatus.Running;
        _ = Run().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task Pause()
    {
        CheckDisposed();
        
        await base.Pause().ConfigureAwait(false);

        Status = ParallelizerStatus.Pausing;
        _savedDop = DegreeOfParallelism;
        await ChangeDegreeOfParallelism(0).ConfigureAwait(false);
        Status = ParallelizerStatus.Paused;
        Stopwatch.Stop();
    }

    /// <inheritdoc/>
    public override async Task Resume()
    {
        CheckDisposed();
        
        await base.Resume().ConfigureAwait(false);

        Status = ParallelizerStatus.Resuming;
        await ChangeDegreeOfParallelism(_savedDop).ConfigureAwait(false);
        Status = ParallelizerStatus.Running;
        Stopwatch.Start();
    }

    /// <inheritdoc/>
    public override async Task Stop()
    {
        CheckDisposed();
        
        await base.Stop().ConfigureAwait(false);

        Status = ParallelizerStatus.Stopping;
        SoftCTS.Cancel();
        await WaitCompletion().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task Abort()
    {
        CheckDisposed();
        
        await base.Abort().ConfigureAwait(false);

        Status = ParallelizerStatus.Stopping;
        HardCTS.Cancel();
        SoftCTS.Cancel();
        await WaitCompletion().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task ChangeDegreeOfParallelism(int newValue)
    {
        CheckDisposed();
        
        await base.ChangeDegreeOfParallelism(newValue);

        if (Status == ParallelizerStatus.Idle)
        {
            DegreeOfParallelism = newValue;
            return;
        }

        if (Status == ParallelizerStatus.Paused)
        {
            _savedDop = newValue;
            return;
        }

        if (newValue == DegreeOfParallelism)
        {
            return;
        }

        if (newValue > DegreeOfParallelism)
        {
            _semaphore.Release(newValue - DegreeOfParallelism);
        }
        else
        {
            _dopDecreaseRequested = true;
            for (var i = 0; i < DegreeOfParallelism - newValue; ++i)
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
            }

            _dopDecreaseRequested = false;
        }

        DegreeOfParallelism = newValue;
    }

    private async Task Run()
    {
        using var items = WorkItems.Skip(Skip).GetEnumerator();
        while (_queue.Count < BatchSize && items.MoveNext())
        {
            _queue.Enqueue(items.Current);
        }
        try
        {
            await ProcessItems(items).ConfigureAwait(false);
        
            // Wait for remaining tasks to finish
            while (Progress < 1 && !HardCTS.IsCancellationRequested)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Wait for the current tasks to finish
            while (_semaphore.CurrentCount < DegreeOfParallelism && !HardCTS.IsCancellationRequested)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            OnError(ex);
        }
        finally
        {
            OnCompleted();
            Stopwatch.Stop();
            _semaphore.Dispose();
            Dispose();
        }
    }
    
    private async Task ProcessItems(IEnumerator<TInput> items)
    {
        while (!_queue.IsEmpty && !SoftCTS.IsCancellationRequested)
        {
            WAIT:
    
            // Wait for the semaphore
            await _semaphore.WaitAsync(SoftCTS.Token).ConfigureAwait(false);
    
            if (SoftCTS.IsCancellationRequested)
                break;
    
            if (_dopDecreaseRequested || IsCPMLimited())
            {
                UpdateCPM();
                _semaphore.Release();
                goto WAIT;
            }
    
            // If we can dequeue an item, run it
            if (_queue.TryDequeue(out var item))
            {
                // The task will release its slot no matter what
                _ = TaskFunction.Invoke(item)
                    .ContinueWith(_ => _semaphore.Release())
                    .ConfigureAwait(false);
            }
            else
            {
                _semaphore.Release();
            }
            
            // If the current batch is not running out
            if (_queue.Count >= MaxDegreeOfParallelism) continue;

            // _queue more items until the BatchSize is reached OR until the enumeration finished
            while (_queue.Count < BatchSize && items.MoveNext())
            {
                _queue.Enqueue(items.Current);
            }
        }
    }
}