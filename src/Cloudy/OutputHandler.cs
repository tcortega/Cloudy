using Cloudy.Models.Checker;
using Cloudy.Models.Data;

namespace Cloudy;

public abstract class OutputHandler<TInput> where TInput : ICredential
{
    internal readonly Func<BotData<TInput>, CheckResult, Task>? AfterOutputAsync;

    protected OutputHandler(Func<BotData<TInput>, CheckResult, Task>? afterOutputAsync)
    {
        AfterOutputAsync = afterOutputAsync;
    }
    public abstract string Directory { get; }
    public abstract void OutputFunc(BotData<TInput> botData, CheckResult checkResult);
}