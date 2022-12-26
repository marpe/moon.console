using System.Collections;

namespace MyGame.Utils;

public class RingBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _end;
    private int _start;

    public RingBuffer(int capacity)
    {
        Capacity = capacity;
        _buffer = new T[capacity];
    }

    public int Count { get; private set; }

    public int Capacity { get; }

    public bool IsEmpty => Count == 0;

    public T this[int index]
    {
        get
        {
            if (IsEmpty)
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");

            if (index >= Count)
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {Count}");

            var actualIndex = InternalIndex(index);
            return _buffer[actualIndex];
        }
        set
        {
            if (IsEmpty)
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");

            if (index >= Count)
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {Count}");

            var actualIndex = InternalIndex(index);
            _buffer[actualIndex] = value;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            var actualIndex = InternalIndex(i);
            yield return _buffer[actualIndex];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item)
    {
        _buffer[_end] = item;
        _end = (_end + 1) % Capacity;
        if (Count == Capacity)
            _start = _end;
        else
            Count++;
    }

    private int InternalIndex(int index)
    {
        var actualIndex = _start + index;
        if (actualIndex < Capacity)
            return actualIndex;
        return -Capacity + actualIndex;
    }
}
