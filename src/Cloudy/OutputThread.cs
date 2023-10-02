using System.Collections.Concurrent;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;

namespace Cloudy;

/// <summary>
/// Represents a thread for handling output with a specified delay between iterations.
/// </summary>
/// <typeparam name="TInput">The type of credential the thread will be working with.</typeparam>
public class OutputThread<TInput> where TInput : ICredential
{
    private readonly List<OutputHandler<TInput>> _outputHandlers;
    private readonly CancellationToken _cancellationToken;
    private readonly ConcurrentBag<(BotData<TInput>, CheckResult)> _items = new();
    private readonly TimeSpan _delay;

    public bool HasPendingItems => !_items.IsEmpty;
    
    /// <summary>
    /// Initializes a new instance of the OutputThread class with the specified parameters.
    /// </summary>
    /// <param name="outputHandler">An instance of a class that inherits from <see cref="OutputHandler{T}"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="delay">The delay between handling output items. Default value is 20 milliseconds.</param>
    public OutputThread(List<OutputHandler<TInput>> outputHandlerses, CancellationToken cancellationToken, TimeSpan? delay = null)
    {
        _outputHandlers = outputHandlerses;
        _cancellationToken = cancellationToken;
        _delay = delay ?? TimeSpan.FromMilliseconds(20);
    }

    /// <summary>
    /// Adds an item to the thread for processing.
    /// </summary>
    /// <param name="botData">The bot data for the output.</param>
    /// <param name="result">The result of the check operation.</param>
    public void AddItem(BotData<TInput> botData, CheckResult result)
    {
        _items.Add((botData, result));
    }

    /// <summary>
    /// Starts the thread to handle output items asynchronously.
    /// </summary>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public async Task StartAsync()
    {
        try
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_delay, _cancellationToken);
                if (_items.IsEmpty) continue;

                ProcessItems();
            }
        }
        catch (OperationCanceledException)
        {
            ProcessItems();
        }
    }

    /// <summary>
    /// Process the items in the thread, invoking the output function for each item.
    /// </summary>
    private void ProcessItems()
    {
        while (_items.TryTake(out var item))
        {
            foreach (var outputHandler in _outputHandlers)
            {
                outputHandler.OutputFuncAsync(item.Item1, item.Item2);
                outputHandler.AfterOutputAsync?.Invoke(item.Item1, item.Item2);
            }
        }
    }
}