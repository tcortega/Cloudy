using Cloudy.Models.Checker;
using Cloudy.Models.Data;

namespace Cloudy.Test.Data;

public class SampleOutputHandler : OutputHandler<SampleCredential>
{
    public int ReceivedItems { get; private set; }
    public int ReceivedHits { get; private set; }
    public int ReceivedCustom { get; private set; }
    public int ReceivedInvalids { get; private set; }
    public int ReceivedRetries { get; private set; }
    public int ReceivedErrors { get; private set; }
    
    public SampleOutputHandler(Func<BotData<SampleCredential>, CheckResult, Task>? afterOutputAsync = null) 
        : base(afterOutputAsync)
    {
    }

    public override Task OutputFuncAsync(BotData<SampleCredential> botData, CheckResult checkResult)
    {
        ReceivedItems++;

        switch (checkResult.Status)
        {
            case CheckResultStatus.Hit:
                ReceivedHits++;
                break;
            case CheckResultStatus.Custom:
                ReceivedCustom++;
                break;
            case CheckResultStatus.Invalid:
                ReceivedInvalids++;
                break;
            case CheckResultStatus.Retry:
                ReceivedRetries++;
                break;
            case CheckResultStatus.Error:
                ReceivedErrors++;
                break;
        }

        return Task.CompletedTask;
    }
}