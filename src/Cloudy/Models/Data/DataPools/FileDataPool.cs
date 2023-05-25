namespace Cloudy.Models.Data.DataPools;

public class FileDataPool : DataPool
{
    public string FilePath { get; private set; }

    /// <summary>
    /// Creates a DataPool by loading lines from a file with the given <paramref name="filePath"/>.
    /// </summary>
    public FileDataPool(string filePath)
    {
        FilePath = filePath;
        DataList = File.ReadLines(filePath);
        Size = DataList.Count();
    }
}