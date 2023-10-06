using Cloudy.Common;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;
using Cloudy.Test.Data;
using FluentAssertions;

namespace Cloudy.Test;

public class ComboCheckerTests
{
    private const int ThreadCount = 1;
    private readonly CheckerDelegate<ComboCredential> _checkerFunc = (_, _) => Task.FromResult(CheckResult.FromHit());

    private readonly List<string> _fakeData = new() { "test:test", "test 2:test" };
    private CollectionDataPool DataPool => new(_fakeData);
    
    [Fact]
    public async Task CheckerShouldBeOutputtedCorrectly()
    {
        var outputHandler = new SampleOutputHandler<ComboCredential>();
        var checker = new ComboCheckerBuilder(new(ThreadCount, ThreadCount), DataPool, _checkerFunc)
            .AddOutputHandler(outputHandler)
            .Build();

        await checker.Start();
        await checker.WaitCompletion();

        await Task.Delay(250);
        outputHandler.ReceivedHits.Should().Be(2);
    }
}