namespace Cloudy.Models.Data.DataPools;

public abstract class DataPool
{
    /// <summary>The IEnumerable of all available data lines.</summary>
    public IEnumerable<string> DataList { get; protected set; }

    /// <summary>The total number of lines.</summary>
    public long Size { get; protected set; }
}