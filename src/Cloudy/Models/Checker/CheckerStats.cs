using Cloudy.Models.Data;
using RuriLib.Parallelization;

namespace Cloudy.Models.Checker;

public class CheckerStats<TInput> where TInput : BotData<ICredential>
{
    private readonly Parallelizer<TInput, CheckResult> _parallelizer;
    private readonly CheckerInfo _cloudyInfo;

    public CheckerStats(Parallelizer<TInput, CheckResult> parallelizer, CheckerInfo cloudyInfo)
    {
        _parallelizer = parallelizer;
        _cloudyInfo = cloudyInfo;
    }


    public int CPM => _parallelizer.CPM;
    public DateTime StartTime => _parallelizer.StartTime;
    public DateTime ETA => _parallelizer.ETA;
    public DateTime? EndTime => _parallelizer.EndTime;
    public long Hits => _cloudyInfo.Hits;
    public long Custom => _cloudyInfo.Custom;
    public long Retries => _cloudyInfo.Retries;
    public long Errors => _cloudyInfo.Errors;
    public long Invalid => _cloudyInfo.Invalid;
    public long Checked => _cloudyInfo.Hits + _cloudyInfo.Custom + _cloudyInfo.Errors + _cloudyInfo.Invalid;
}