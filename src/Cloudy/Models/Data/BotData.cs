using Cloudy.Http;

namespace Cloudy.Models.Data;

/// <summary>
/// The BotData the Checker will use to check the credential.
/// </summary>
/// <typeparam name="TInput">The type of credential the bot data is expected to hold.</typeparam>
public class BotData<TInput> where TInput : ICredential
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BotData{TInput}"/> class.
    /// </summary>
    /// <param name="parseResult">The <see cref="IDataParseResult{TInput}"/> of the credential.</param>
    public BotData(IDataParseResult<TInput> parseResult)
    {
        ParseResult = parseResult;
    }

    /// <summary>
    /// The number of attempts the checker has made for this credential.
    /// </summary>
    public int Attempts { get; internal set; }

    /// <summary>
    /// The <see cref="IDataParseResult{TInput}"/> of the credential.
    /// </summary>
    public IDataParseResult<TInput> ParseResult { get; set; }

    /// <summary>
    /// The <see cref="TInput" />.
    /// </summary>
    public TInput Input => ParseResult.Value;

    /// <summary>
    /// The static <see cref="CloudyHttpClient"/> available for unproxified http calls.
    /// </summary>
    public static CloudyHttpClient UnproxifiedClient { get; set; } = null!;

    /// <summary>
    /// The CancellationToken from the <see cref="Parallelizer{TInput,TOutput}"/>
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}
