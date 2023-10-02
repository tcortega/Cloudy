using Cloudy.Models.Checker;
using Cloudy.Models.Data;

namespace Cloudy;

/// <summary>
/// The base abstract class for handling output.
/// </summary>
/// <typeparam name="TInput">The type of credential the handler will be working with.</typeparam>
public abstract class OutputHandler<TInput> where TInput : ICredential
{
    /// <summary>
    /// An optional asynchronous function that runs after the output is complete.
    /// </summary>
    internal readonly Func<BotData<TInput>, CheckResult, Task>? AfterOutputAsync;

    /// <summary>
    /// Initializes a new instance of the OutputHandler class with the specified after output function.
    /// </summary>
    /// <param name="afterOutputAsync">The function to run after the output is complete.</param>
    protected OutputHandler(Func<BotData<TInput>, CheckResult, Task>? afterOutputAsync)
    {
        AfterOutputAsync = afterOutputAsync;
    }

    /// <summary>
    /// Defines the function for outputting results.
    /// </summary>
    /// <param name="botData">The bot data used in the checking process.</param>
    /// <param name="checkResult">The result sent from the Checker.</param>
    public abstract Task OutputFuncAsync(BotData<TInput> botData, CheckResult checkResult);
}