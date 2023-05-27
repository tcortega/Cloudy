using Cloudy.Http;

namespace Cloudy.Models.Data;

public class BotData<TInput> where TInput : ICredential
{
    public BotData(IDataParseResult<TInput> input)
    {
        Input = input;
    }

    public int Attempts { get; internal set; }
    public IDataParseResult<TInput> Input { get; set; }
    public static CloudyHttpClient UnproxifiedClient { get; set; } = null!;
    public CancellationToken CancellationToken { get; set; }
}