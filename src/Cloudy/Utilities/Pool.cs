using System.Collections.Concurrent;

namespace Cloudy.Utilities;

public class Pool<T>
{
    protected readonly BlockingCollection<T> Items = new();
    protected readonly int MaxSize;

    protected Pool(int maxSize)
    {
        MaxSize = maxSize;
    }

    public static Pool<T> Create(IEnumerable<T> items, int maxSize)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize));

        var itemList = items.ToList();
        if (itemList.Count > maxSize)
        {
            throw new ArgumentException("Initial item count cannot be greater than the maximum pool size.");
        }

        var pool = new Pool<T>(maxSize);
        pool.Fill(itemList);
        return pool;
    }

    protected void Fill(IReadOnlyList<T> items)
    {
        foreach (var item in items)
        {
            Items.Add(item);
        }
        
        var i = 0;
        while (Items.Count < MaxSize)
        {
            Items.Add(items[i++ % items.Count]);
        }
    }

    public T Borrow(CancellationToken cancellationToken = default)
    {
        return Items.Take(cancellationToken);
    }

    public void Return(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        Items.Add(item);
    }
}