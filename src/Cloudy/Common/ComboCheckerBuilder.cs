using Cloudy.Models.Checker;
using Cloudy.Models.Data;
using Cloudy.Models.Data.DataPools;

namespace Cloudy.Common;

public class ComboCheckerBuilder : CheckerBuilder<ComboCredential>
{
    public ComboCheckerBuilder(CheckerSettings settings, DataPool dataPool, CheckerDelegate<ComboCredential> checkerFunc) 
        : base(settings, dataPool, ComboCredential.Parser, checkerFunc)
    {
    }
}