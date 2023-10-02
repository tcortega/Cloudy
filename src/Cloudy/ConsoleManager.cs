using System.Text;
using Cloudy.Models.Checker;

namespace Cloudy;

public class ConsoleManager
{
    private readonly CheckerInfo _checkerInfo;

    public ConsoleManager(CheckerInfo checkerInfo)
    {
        _checkerInfo = checkerInfo;
    }

    /// <summary>
    /// Will start updating your <see cref="Console.Title"/> using your <see cref="CheckerInfo"/> statistics
    /// </summary>
    /// <param name="updateInterval">Interval between title updates</param>
    /// <param name="customDisplay">The <see cref="CheckResultStatus.Custom"/> custom display text to appear in the title.</param>
    /// <param name="showCustom">Whether you want <see cref="CheckResultStatus.Custom"/> to be shown</param>
    /// <param name="showPercentages">Whether you want some cool percentages to be shown</param>
    /// <param name="prefix">Prefix to add to title, can be useful as something like "Milky Checker v1.0.0 by Laiteux — "</param>
    /// <param name="suffix">Suffix to add to title, can be useful for uh idk</param>
    public async Task StartUpdatingTitleAsync(TimeSpan updateInterval, string customDisplay = "Custom", bool showCustom = true, bool showPercentages = true, string? prefix = null, string? suffix = null)
    {
        while (true)
        {
            var titleBuilder = new StringBuilder().Append(prefix).Append(_checkerInfo.Status);

            if (_checkerInfo.Status != CheckerStatus.Idle)
            {
                titleBuilder.Append(" | ");
                titleBuilder.Append($"Checked: {_checkerInfo.Checked:N0} ({_checkerInfo.Progress:P})");
                titleBuilder.Append(" — ");
                titleBuilder.Append($"Hits: {_checkerInfo.Hits:N0} ({(double) _checkerInfo.Hits / _checkerInfo.Checked:P2})");
                titleBuilder.Append(" — ");
                titleBuilder.Append($"{customDisplay}: {_checkerInfo.Custom:N0} ({(double) _checkerInfo.Custom / _checkerInfo.Checked:P2})");
                titleBuilder.Append(" | ");
                titleBuilder.Append($"CPM: {_checkerInfo.CPM}");
                titleBuilder.Append(" — ");
                titleBuilder.Append($"Elapsed: {_checkerInfo.Elapsed.ToString() ?? "?"}");
                titleBuilder.Append(" — ");
                titleBuilder.Append($"Remaining: {_checkerInfo.Remaining.ToString() ?? "?"}");
            }

            Console.Title = titleBuilder.Append(suffix).ToString();

            if (_checkerInfo.Status == CheckerStatus.Completed)
            {
                break;
            }

            await Task.Delay(updateInterval).ConfigureAwait(false);
        }
    }

    // /// <summary>
    // /// Will start listening to user keys and do actions associated with them when pressed
    // /// </summary>
    // /// <param name="pause"><see cref="ConsoleKey"/> for <see cref="CheckerStatus.Paused"/>, null to disable</param>
    // /// <param name="resume"><see cref="ConsoleKey"/> for <see cref="CheckerStatus.Running"/>, null to disable</param>
    // /// /// <param name="saveUnchecked"><see cref="ConsoleKey"/> for saving unchecked combos, null to disable</param>
    // /// <param name="abort"><see cref="ConsoleKey"/> for <see cref="CheckerStatus.Idle"/>, null to disable</param>
    // public async Task StartListeningKeysAsync(ConsoleKey? pause = ConsoleKey.P, ConsoleKey? resume = ConsoleKey.R, ConsoleKey? saveUnchecked = ConsoleKey.S, ConsoleKey? abort = null)
    // {
    //     while (_checkerInfo.Status != CheckerStatus.Completed)
    //     {
    //         if (!Console.KeyAvailable)
    //         {
    //             await Task.Delay(100).ConfigureAwait(false); // I'm not sure if this is the best practice
    //
    //             continue;
    //         }
    //
    //         if (_checkerInfo.Status == CheckerStatus.Idle)
    //         {
    //             continue; // We don't want to do anything if checker is idle
    //         }
    //
    //         ConsoleKey key = Console.ReadKey(true).Key;
    //
    //         if ((pause != null && key == pause) || (resume != null && key == resume))
    //         {
    //             if (_checkerInfo.Status == CheckerStatus.Running)
    //             {
    //                 _checker.Pause();
    //
    //                 lock (_checkerInfo.Locker)
    //                 {
    //                     Console.ForegroundColor = ConsoleColor.White;
    //                     Console.WriteLine($"{Environment.NewLine}Checker paused, press \"{resume}\" to resume...{Environment.NewLine}");
    //                 }
    //             }
    //             else if (_checkerInfo.Status == CheckerStatus.Paused)
    //             {
    //                 lock (_checkerInfo.Locker)
    //                 {
    //                     if (_checkerInfo.LastHit > _checkerInfo.LastPause)
    //                     {
    //                         Console.WriteLine();
    //                     }
    //
    //                     Console.ForegroundColor = ConsoleColor.White;
    //                     Console.WriteLine($"Checker resumed! Pause duration: {TimeSpan.FromSeconds((int)_checker.Resume().TotalSeconds)}{Environment.NewLine}");
    //                 }
    //             }
    //         }
    //         else if (saveUnchecked != null && key == saveUnchecked)
    //         {
    //             lock (_checkerInfo.Locker)
    //             {
    //                 Console.ForegroundColor = ConsoleColor.White;
    //                 Console.WriteLine($"{Environment.NewLine}Saving unchecked combos, please wait...{Environment.NewLine}");
    //             }
    //
    //             var saveStart = DateTime.Now;
    //
    //             int @unchecked = _checker.SaveUnchecked();
    //
    //             lock (_checkerInfo.Locker)
    //             {
    //                 if (_checkerInfo.LastHit > saveStart)
    //                 {
    //                     Console.WriteLine();
    //                 }
    //
    //                 Console.ForegroundColor = ConsoleColor.White;
    //                 Console.WriteLine($"Saved {@unchecked:N0} unchecked combos!{Environment.NewLine}");
    //             }
    //         }
    //         else if (abort != null && key == abort)
    //         {
    //             _checker.Abort();
    //         }
    //     }
}