using System.Collections.Concurrent;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;

namespace Cloudy;

public class OutputThread<T> where T : ICredential
{
    private readonly OutputHandler<T> _outputHandler;
    private readonly CancellationToken _cancellationToken;
    private readonly ConcurrentBag<(BotData<T>, CheckResult)> _items = new();
    private readonly TimeSpan _delay;

    public OutputThread(OutputHandler<T> outputHandler, CancellationToken cancellationToken, TimeSpan? delay = null)
    {
        _outputHandler = outputHandler;
        _cancellationToken = cancellationToken;
        _delay = delay ?? TimeSpan.FromMilliseconds(20);
    }

    public void AddItem(BotData<T> botData, CheckResult result)
    {
        _items.Add((botData, result));
    }

    public async Task StartAsync()
    {
        try
        {
            while (true)
            {
                await Task.Delay(_delay, _cancellationToken);
                if (_items.IsEmpty) continue;

                while (_items.TryTake(out var item))
                {
                    _outputHandler.OutputFunc(item.Item1, item.Item2);
                    _outputHandler.AfterOutputAsync?.Invoke(item.Item1, item.Item2);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }
}