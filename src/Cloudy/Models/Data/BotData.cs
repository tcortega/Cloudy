using Cloudy.Http;

namespace Cloudy.Models.Data;

public class BotData<TInput> where TInput : ICredential
{
    public int Attempts = 0;
    
    public BotData(IDataParseResult<TInput> input)
    {
        Input = input;
    }

    public IDataParseResult<TInput> Input { get; set; }
    public static CloudyHttpClient UnproxifiedClient { get; set; } = null!;
    public CancellationToken CancellationToken { get; set; }
}