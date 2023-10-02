using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cloudy.Parallelization.Exceptions;
using Cloudy.Parallelization.Models;

namespace Cloudy.Parallelization;

/// <summary>
/// Provides a managed way to execute parallelized work.
/// </summary>
/// <typeparam name="TInput">The type of the workload items</typeparam>
/// <typeparam name="TOutput">The type of the results</typeparam>
public abstract class Parallelizer<TInput, TOutput> : IDisposable, IParallelizerStats
{
    public bool IsDisposed { get; protected set; }
    
    /// <summary>
    /// The maximum value that the degree of parallelism can have when changed through the
    /// <see cref="Parallelizer{TInput, TOutput}.ChangeDegreeOfParallelism(int)"/> method.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// The current status of the parallelizer.
    /// </summary>
    public ParallelizerStatus Status
    {
        get => InternalStatus;
        protected set
        {
            InternalStatus = value;
            OnStatusChanged(InternalStatus);
        }
    }

    /// <summary>
    /// Retrieves the current progress in the interval [0, 1].
    /// The progress is -1 if the manager hasn't been started yet.
    /// </summary>
    public float Progress => (float)(Processed + Skip) / TotalAmount;

    /// <summary>
    /// Retrieves the completed work per minute.
    /// </summary>
    public int CPM { get; protected set; }

    /// <summary>
    /// Sets a maximum threshold for CPM. 0 to disable.
    /// </summary>
    public int CPMLimit { get; set; }

    /// <summary>
    /// The time when the parallelizer started its work for its last running session.
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// The time when the parallelizer finished its work or was stopped (<see langword="null"/> if it hasn't finished
    /// a single session yet).
    /// </summary>
    public DateTime? EndTime { get; private set; }
        
    /// <summary>
    /// The Estimated Time of Arrival (when the parallelizer is expected to finish all the work).
    /// </summary>
    public DateTime ETA
    {
        get
        {
            var minutes = TotalAmount * (1 - Progress) / CPM;
            return CPM > 0 && minutes < TimeSpan.MaxValue.TotalMinutes
                ? StartTime + TimeSpan.FromMinutes(minutes)
                : DateTime.MaxValue;
        }
    }

    /// <summary>
    /// The time elapsed since the start of the session.
    /// </summary>
    public TimeSpan Elapsed => TimeSpan.FromMilliseconds(Stopwatch.ElapsedMilliseconds);

    /// <summary>
    /// The expected remaining time to finish all the work.
    /// </summary>
    public TimeSpan Remaining => EndTime.HasValue ? TimeSpan.Zero : ETA - DateTime.Now;

    /// <summary>
    /// The status of the parallelizer.
    /// </summary>
    protected ParallelizerStatus InternalStatus = ParallelizerStatus.Idle;

    /// <summary>
    /// The number of items that can be processed concurrently.
    /// </summary>
    protected int DegreeOfParallelism;

    /// <summary>
    /// The items to process.
    /// </summary>
    protected readonly IEnumerable<TInput> WorkItems;

    /// <summary>
    /// The function to process items and get results.
    /// </summary>
    protected readonly Func<TInput, CancellationToken, Task<TOutput>> WorkFunction;

    /// <summary>
    /// The function that turns each input item into an awaitable <see cref="Task"/>.
    /// </summary>
    protected readonly Func<TInput, Task> TaskFunction;

    /// <summary>
    /// The total amount of work items that are expected to be enumerated (for progress calculations).
    /// </summary>
    protected readonly long TotalAmount;

    /// <summary>
    /// The number of items to skip from the start of the collection (to restore previously aborted sessions).
    /// </summary>
    protected readonly int Skip;

    /// <summary>
    /// The current amount of work items that were processed so far.
    /// </summary>
    protected int Processed;

    /// <summary>
    /// The list of timestamps for CPM calculation.
    /// </summary>
    protected List<int> CheckedTimestamps = new();

    /// <summary>
    /// A lock that can be used to update the CPM from a single thread at a time.
    /// </summary>
    protected readonly object CPMLock = new();

    /// <summary>
    /// The stopwatch that calculates the elapsed time.
    /// </summary>
    protected readonly Stopwatch Stopwatch = new();

    /// <summary>
    /// A soft cancellation token. Cancel this for soft AND hard abort.
    /// </summary>
    protected CancellationTokenSource SoftCTS;

    /// <summary>
    /// A hard cancellation token. Cancel this for hard abort only.
    /// </summary>
    protected CancellationTokenSource HardCTS;
    
    /// <summary>Called when an operation throws an exception.</summary>
    public event EventHandler<ErrorDetails<TInput>>? TaskError;

    /// <summary>
    /// Invokes a <see cref="Parallelizer{TInput, TOutput}.TaskError"/> event.
    /// </summary>
    protected virtual void OnTaskError(ErrorDetails<TInput> input) => TaskError?.Invoke(this, input);

    /// <summary>Called when the <see cref="Parallelizer{TInput, TOutput}"/> itself throws an exception.</summary>
    public event EventHandler<Exception>? Error;

    /// <summary>
    /// Invokes a <see cref="Parallelizer{TInput, TOutput}.Error"/> event.
    /// </summary>
    protected virtual void OnError(Exception ex) => Error?.Invoke(this, ex);

    /// <summary>Called when an operation is completed successfully.</summary>
    public event EventHandler<ResultDetails<TInput, TOutput>>? NewResult;

    /// <summary>
    /// Invokes a <see cref="Parallelizer{TInput, TOutput}.NewResult"/> event.
    /// </summary>
    protected virtual void OnNewResult(ResultDetails<TInput, TOutput> result) => NewResult?.Invoke(this, result);

    /// <summary>Called when the progress changes.</summary>
    public event EventHandler<float>? ProgressChanged;

    /// <summary>
    /// Invokes a <see cref="Parallelizer{TInput, TOutput}.ProgressChanged"/> event.
    /// </summary>
    protected virtual void OnProgressChanged(float progress) => ProgressChanged?.Invoke(this, progress);

    /// <summary>Called when all operations were completed successfully.</summary>
    public event EventHandler? Completed;

    /// <summary>
    /// Invokes a <see cref="Parallelizer{TInput, TOutput}.Completed"/> event.
    /// </summary>
    protected virtual void OnCompleted() 
    {
        Status = ParallelizerStatus.Idle;
        Completed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called when <see cref="Status"/> changes.</summary>
    public event EventHandler<ParallelizerStatus>? StatusChanged;

    /// <summary>
    /// Invokes a <see cref="Parallelizer{TInput, TOutput}.StatusChanged"/> event.
    /// </summary>
    protected virtual void OnStatusChanged(ParallelizerStatus newStatus) => StatusChanged?.Invoke(this, newStatus);

    /// <summary>
    /// Creates a new instance of <see cref="Parallelizer{TInput, TOutput}"/>.
    /// </summary>
    /// <param name="workItems">The collection of data to process in parallel</param>
    /// <param name="workFunction">The work function that must be executed on the data</param>
    /// <param name="degreeOfParallelism">The amount of concurrent tasks that can be started</param>
    /// <param name="totalAmount">The total amount of data that is expected from <paramref name="workItems"/></param>
    /// <param name="skip">The amount of <paramref name="workItems"/> to skip at the beginning</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism that can be set</param>
    protected Parallelizer(IEnumerable<TInput> workItems, Func<TInput, CancellationToken, Task<TOutput>> workFunction,
        int degreeOfParallelism, long totalAmount, int skip = 0, int maxDegreeOfParallelism = 200)
    {
        if (degreeOfParallelism < 1)
            throw new ArgumentException("The degree of parallelism must be greater than 1");

        if (degreeOfParallelism > maxDegreeOfParallelism)
            throw new ArgumentException("The degree of parallelism must not be greater than the maximum degree of parallelism");

        if (skip >= totalAmount)
            throw new ArgumentException("The skip must be less than the total amount");

        WorkItems = workItems ?? throw new ArgumentNullException(nameof(workItems));
        WorkFunction = workFunction ?? throw new ArgumentNullException(nameof(workFunction));
        TotalAmount = totalAmount;
        DegreeOfParallelism = degreeOfParallelism;
        Skip = skip;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
        SoftCTS = new();
        HardCTS = new();

        // Assign the task function
        TaskFunction = async item =>
        {
            if (SoftCTS.IsCancellationRequested)
                return;

            // Try to execute the work and report the result
            try
            {
                var workResult = await workFunction.Invoke(item, HardCTS.Token).ConfigureAwait(false);
                OnNewResult(new(item, workResult));
                HardCTS.Token.ThrowIfCancellationRequested();
            }
            // Catch and report any exceptions
            catch (Exception ex)
            {
                OnTaskError(new(item, ex));
            }
            // Report the progress, update the CPM and release the semaphore slot
            finally
            {
                Interlocked.Increment(ref Processed);
                OnProgressChanged(Progress);

                CheckedTimestamps.Add(Environment.TickCount);
                UpdateCPM();
            }
        };
    }

    /// <summary>
    /// Starts the execution (without waiting for completion).
    /// </summary>
    public virtual Task Start()
    {
        CheckDisposed();
        
        if (Status != ParallelizerStatus.Idle)
            throw new RequiredStatusException(ParallelizerStatus.Idle, Status);

        StartTime = DateTime.Now;
        EndTime = null;
        CheckedTimestamps.Clear();

        SoftCTS = new();
        HardCTS = new();

        return Task.CompletedTask;
    }

    /// <summary>Pauses the execution (waits until the ongoing operations are completed).</summary>
    public virtual Task Pause()
    {
        if (Status != ParallelizerStatus.Running)
            throw new RequiredStatusException(ParallelizerStatus.Running, Status);

        return Task.CompletedTask;
    }

    /// <summary>Resumes a paused execution.</summary>
    public virtual Task Resume()
    {
        if (Status != ParallelizerStatus.Paused)
            throw new RequiredStatusException(ParallelizerStatus.Paused, Status);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the execution (waits for the current items to finish).
    /// </summary>
    public virtual Task Stop()
    {
        if (Status != ParallelizerStatus.Running && Status != ParallelizerStatus.Paused)
            throw new RequiredStatusException(new[] { ParallelizerStatus.Running, ParallelizerStatus.Paused }, Status);

        EndTime = DateTime.Now;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Aborts the execution without waiting for the current work to finish.
    /// </summary>
    public virtual Task Abort()
    {
        if (Status != ParallelizerStatus.Running && Status != ParallelizerStatus.Paused && Status != ParallelizerStatus.Stopping
            && Status != ParallelizerStatus.Pausing)
            throw new RequiredStatusException(new[]
                    { ParallelizerStatus.Running, ParallelizerStatus.Paused, ParallelizerStatus.Stopping, ParallelizerStatus.Pausing},
                Status);

        EndTime = DateTime.Now;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dynamically changes the degree of parallelism.
    /// </summary>
    public virtual Task ChangeDegreeOfParallelism(int newValue)
    {
        // This can be 0 because we can use 0 dop as a pausing system
        if (newValue < 0 || newValue > MaxDegreeOfParallelism)
            throw new ArgumentException($"Must be within 0 and {MaxDegreeOfParallelism}", nameof(newValue));

        return Task.CompletedTask;
    }

    /// <summary>
    /// An awaitable handler that completes when the <see cref="Status"/> is <see cref="ParallelizerStatus.Idle"/>.
    /// </summary>
    public async Task WaitCompletion(CancellationToken cancellationToken = default)
    {
        while (Status != ParallelizerStatus.Idle)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Whether the CPM is limited to a certain amount (for throttling purposes).
    /// </summary>
    /// <returns></returns>
    protected bool IsCPMLimited() => CPMLimit > 0 && CPM > CPMLimit;

    /// <summary>
    /// Updates the CPM (safe to be called from multiple threads).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void UpdateCPM()
    {
        if (!Monitor.TryEnter(CPMLock)) return;

        try
        {
            CheckedTimestamps = CheckedTimestamps.Where(t => Environment.TickCount - t < 60000).ToList();
            CPM = CheckedTimestamps.Count;                                
        }
        finally
        {
            Monitor.Exit(CPMLock);
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of <see
    /// cref="Parallelizer{TInput, TOuput}"/>.
    /// </summary>
    public virtual void Dispose()
    {
        SoftCTS.Dispose();
        HardCTS.Dispose();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    protected void CheckDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException("The current instance of Parallelizer has already been disposed");
        }
    }
}