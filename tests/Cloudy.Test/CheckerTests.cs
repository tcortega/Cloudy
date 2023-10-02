using Cloudy.Models.Checker;
using Cloudy.Test.Data;
using FluentAssertions;

namespace Cloudy.Test;

public class CheckerTests
{
    private const int ThreadCount = 1;
    private readonly CheckerDelegate<SampleCredential> _checkerFunc = (data, _) =>
    {
        return Task.FromResult(data.ParseResult.Value.ExpectedStatus switch
        {
            CheckResultStatus.Custom => CheckResult.FromCustom(),
            CheckResultStatus.Hit => CheckResult.FromHit(),
            CheckResultStatus.Invalid => CheckResult.FromInvalid(),
            CheckResultStatus.Retry => CheckResult.FromRetry(),
            _ => CheckResult.FromException(new())
        });
    };

    private readonly List<SampleCredential> _fakeData = new()
    {
        new(Guid.NewGuid(), CheckResultStatus.Custom), new(Guid.NewGuid(), CheckResultStatus.Hit), new(Guid.NewGuid(), CheckResultStatus.Invalid), new(Guid.NewGuid(), CheckResultStatus.Retry),
        new(Guid.NewGuid(), CheckResultStatus.Error)
    };
    
    private SampleDataPool DataPool => new(_fakeData.Select(x => $"{x.Id}:{x.ExpectedStatus:D}").ToList());
    
    [Fact]
    public async Task CheckerShouldBeOutputtedCorrectly()
    {
        var outputHandler = new SampleOutputHandler();
        var checker = new CheckerBuilder<SampleCredential>(new(ThreadCount, ThreadCount), DataPool, SampleCredential.Parser, _checkerFunc)
            .AddOutputHandler(outputHandler)
            .Build();

        await checker.Start();
        await checker.WaitCompletion();

        // var checker = new ComboCheckerBuilder(new CheckerSettings(), dataPool, _checkerFunc)
        //     .WithOutputHandler(outputHandler)
        //     .Build();
        //
        // checker.StartAsync().Wait();
        //

        await Task.Delay(25);
        outputHandler.ReceivedItems.Should().Be(5);
        outputHandler.ReceivedHits.Should().Be(1);
        outputHandler.ReceivedCustom.Should().Be(1);
        outputHandler.ReceivedInvalids.Should().Be(1);
        outputHandler.ReceivedRetries.Should().Be(1);
        outputHandler.ReceivedErrors.Should().Be(1);
    }
}