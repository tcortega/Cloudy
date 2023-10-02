using Cloudy.Models.Data.DataPools;

namespace Cloudy.Test.Data;

public class SampleDataPool : DataPool
{
    public SampleDataPool(IReadOnlyCollection<string> credentials)
    {
        DataList = credentials;
        Size = credentials.Count;
    }
}