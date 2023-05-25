using Cloudy.Http;
using Cloudy.Models.Checker;
using Cloudy.Models.Data;
using Cloudy.Models.Data.DataPools;

namespace Cloudy.Common;

public class ComboCheckerBuilder : CheckerBuilder<ComboCredential>
{
    public ComboCheckerBuilder(CheckerSettings settings, DataPool dataPool, Func<BotData<ComboCredential>, CloudyHttpClient, Task<CheckResult>> checkerFunc) 
        : base(settings, dataPool, DataParser.ParseCombo, checkerFunc)
    {
    }
}