namespace Cloudy.Models.Data.DataPools;

public class InfiniteDataPool : DataPool
{
    /// <summary>
    /// Creates a DataPool of empty strings that never ends.
    /// </summary>
    public InfiniteDataPool()
    {
        DataList = InfiniteCounter();
        Size = int.MaxValue;
    }

    private static IEnumerable<string> InfiniteCounter()
    {
        while (true) yield return string.Empty;
    }
}