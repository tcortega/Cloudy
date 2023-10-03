using System.Globalization;
using System.Text;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;

namespace Cloudy.Common;

public class FileOutputHandler<TInput> : OutputHandler<TInput> where TInput : ICredential
{
    private readonly string _directory;
    private readonly string _separator;

    public FileOutputHandler(string? directory = null, string resultsFolderName = "results", string separator = " | ",
        Func<BotData<TInput>, CheckResult, Task>? afterOutputAsync = null) : base(afterOutputAsync)
    {
        var folderName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(DateTime.Now.ToString("MMM dd, yyyy — HH.mm.ss"));
        _directory = directory ?? Path.Combine(resultsFolderName, folderName);
        _separator = separator;

        EnsureDirectoryCreated(_directory);
    }

    public override async Task OutputFuncAsync(BotData<TInput> botData, CheckResult checkResult)
    {
        if (checkResult.Status is not (CheckResultStatus.Hit or CheckResultStatus.Custom)) return;

        var outputBuilder = new StringBuilder(botData.ParseResult.Value.Raw);
        if (checkResult.Captures is not null && checkResult.Captures.Count > 0)
        {
            var captures = checkResult.Captures
                .Where(c => !string.IsNullOrWhiteSpace(c.Value.ToString()))
                .Select(c => $"{c.Key} = {c.Value}");

            outputBuilder.Append(_separator).AppendJoin(_separator, captures);
        }

        outputBuilder.Append(Environment.NewLine);

        var outputPath = Path.Combine(_directory, $"{checkResult.Status.ToStringFast()}.txt");
        await File.AppendAllTextAsync(outputPath, outputBuilder.ToString());
    }
    
    private static void EnsureDirectoryCreated(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }
}