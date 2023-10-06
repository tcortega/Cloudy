using Cloudy.Models.Data.DataPools;

namespace Cloudy.Test.Data;

public class CollectionDataPool : DataPool
{
    public CollectionDataPool(IReadOnlyCollection<string> credentials)
    {
        DataList = credentials;
        Size = credentials.Count;
    }
}