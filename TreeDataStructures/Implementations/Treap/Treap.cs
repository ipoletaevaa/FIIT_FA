using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new TreapNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }

    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) { }

    public override void Add(TKey key, TValue value)
    {
        TreapNode<TKey, TValue>? existing = FindNode(key);
        if (existing is not null)
        {
            existing.Value = value;
            return;
        }

        (TreapNode<TKey, TValue>? lower, TreapNode<TKey, TValue>? upper) = PartitionByKey(Root, key);
        TreapNode<TKey, TValue> inserted = CreateNode(key, value);

        TreapNode<TKey, TValue>? leftPart = CombineSubtrees(lower, inserted);
        Root = CombineSubtrees(leftPart, upper);

        if (Root is not null)
        {
            Root.Parent = null;
        }

        Count++;
    }

    public override bool Remove(TKey key)
    {
        TreapNode<TKey, TValue>? target = FindNode(key);
        if (target is null)
        {
            return false;
        }

        TreapNode<TKey, TValue>? merged = CombineSubtrees(target.Left, target.Right);
        Transplant(target, merged);
        Count--;
        return true;
    }

    /// <summary>
    /// Делит поддерево на части: ключи &lt;= pivot и ключи &gt; pivot.
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Lower, TreapNode<TKey, TValue>? Upper) PartitionByKey(
        TreapNode<TKey, TValue>? subtree,
        TKey pivot)
    {
        if (subtree is null)
        {
            return (null, null);
        }

        if (Comparer.Compare(subtree.Key, pivot) <= 0)
        {
            (TreapNode<TKey, TValue>? lowerRight, TreapNode<TKey, TValue>? upper) =
                PartitionByKey(subtree.Right, pivot);

            subtree.Right = lowerRight;
            if (lowerRight is not null)
            {
                lowerRight.Parent = subtree;
            }

            if (upper is not null)
            {
                upper.Parent = null;
            }

            return (subtree, upper);
        }

        (TreapNode<TKey, TValue>? lower, TreapNode<TKey, TValue>? upperLeft) =
            PartitionByKey(subtree.Left, pivot);

        subtree.Left = upperLeft;
        if (upperLeft is not null)
        {
            upperLeft.Parent = subtree;
        }

        if (lower is not null)
        {
            lower.Parent = null;
        }

        return (lower, subtree);
    }

    /// <summary>
    /// Объединяет два поддерева при условии, что все ключи слева меньше ключей справа.
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? CombineSubtrees(
        TreapNode<TKey, TValue>? lower,
        TreapNode<TKey, TValue>? upper)
    {
        if (lower is null)
        {
            return upper;
        }

        if (upper is null)
        {
            return lower;
        }

        if (lower.Priority > upper.Priority)
        {
            lower.Right = CombineSubtrees(lower.Right, upper);
            if (lower.Right is not null)
            {
                lower.Right.Parent = lower;
            }

            return lower;
        }

        upper.Left = CombineSubtrees(lower, upper.Left);
        if (upper.Left is not null)
        {
            upper.Left.Parent = upper;
        }

        return upper;
    }
}
