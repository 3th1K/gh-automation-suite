using System.Collections.Concurrent;

namespace Infrastructure.Utilities;

public class ConcurrentHashSet<T>
{
    private readonly ConcurrentDictionary<T, byte> _dictionary;

    public ConcurrentHashSet()
    {
        _dictionary = new ConcurrentDictionary<T, byte>();
    }

    // Constructor to initialize with a collection
    public ConcurrentHashSet(IEnumerable<T> collection)
    {
        _dictionary = new ConcurrentDictionary<T, byte>();
        foreach (var item in collection)
        {
            _dictionary.TryAdd(item, 0);
        }
    }

    public bool Add(T item) => _dictionary.TryAdd(item, 0);

    public bool Remove(T item) => _dictionary.TryRemove(item, out _);

    public bool Contains(T item) => _dictionary.ContainsKey(item);

    public int Count => _dictionary.Count;

    public List<T> ToList() => _dictionary.Keys.ToList();
}
