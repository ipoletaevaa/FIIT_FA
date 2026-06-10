using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlNode<TKey, TValue> : Node<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    private int _height;

    public AvlNode(TKey key, TValue value)
        : base(key, value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _height = 1;
    }

    public int Height
    {
        get => _height;
        set => _height = value;
    }
}