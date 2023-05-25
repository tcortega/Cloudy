using System.Globalization;
using System.Text;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;

namespace Cloudy.Common;

public class FileOutputHandler<TInput> : OutputHandler<TInput> where TInput : ICredential
{
    public override string Directory { get; }
    private readonly string _separator;

    public FileOutputHandler(string? directory = null, string separator = " | ",
        Func<BotData<TInput>, CheckResult, Task>? afterOutputAsync = null) : base(afterOutputAsync)
    {
        Directory = directory ?? Path.Combine("results", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        _separator = separator;
    }

    public override void OutputFunc(BotData<TInput> botData, CheckResult checkResult)
    {
        if (checkResult.Status is not CheckResultStatus.Hit or CheckResultStatus.Custom) return;

        var outputBuilder = new StringBuilder(botData.Input.Value.Raw);
        if (checkResult.Captures is not null && checkResult.Captures.Count > 0)
        {
            var captures = checkResult.Captures
                .Where(c => !string.IsNullOrWhiteSpace(c.Value.ToString()))
                .Select(c => $"{c.Key} = {c.Value}");

            outputBuilder.Append(_separator).AppendJoin(_separator, captures).Append(Environment.NewLine);
        }

        var outputPath = Path.Combine(Directory, $"{checkResult.Status}.txt");
        File.AppendAllText(outputPath, outputBuilder.ToString());
    }
}