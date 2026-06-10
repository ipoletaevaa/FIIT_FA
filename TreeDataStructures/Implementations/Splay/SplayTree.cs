using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new BstNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        ElevateNode(newNode);
    }

    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent is not null)
        {
            ElevateNode(parent);
            return;
        }

        if (child is not null)
        {
            ElevateNode(child);
        }
    }

    public override bool ContainsKey(TKey key)
    {
        BstNode<TKey, TValue>? located = FindNode(key);
        if (located is null)
        {
            return false;
        }

        ElevateNode(located);
        return true;
    }

    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        BstNode<TKey, TValue>? located = FindNode(key);
        if (located is null)
        {
            value = default;
            return false;
        }

        value = located.Value;
        ElevateNode(located);
        return true;
    }

    private void ElevateNode(BstNode<TKey, TValue>? target)
    {
        if (target is null)
        {
            return;
        }

        while (target.Parent is not null)
        {
            BstNode<TKey, TValue> ancestor = target.Parent;
            BstNode<TKey, TValue>? greatAncestor = ancestor.Parent;

            if (greatAncestor is null)
            {
                RotateWithParent(target, ancestor);
            }
            else if (target.IsLeftChild && ancestor.IsLeftChild)
            {
                RotateLeftLine(target, ancestor, greatAncestor);
            }
            else if (target.IsRightChild && ancestor.IsRightChild)
            {
                RotateRightLine(target, ancestor, greatAncestor);
            }
            else if (target.IsRightChild && ancestor.IsLeftChild)
            {
                RotateLeftRight(target, ancestor, greatAncestor);
            }
            else
            {
                RotateRightLeft(target, ancestor, greatAncestor);
            }
        }

        Root = target;
    }

    private void RotateWithParent(BstNode<TKey, TValue> target, BstNode<TKey, TValue> ancestor)
    {
        if (target.IsLeftChild)
        {
            RotateRight(ancestor);
        }
        else
        {
            RotateLeft(ancestor);
        }
    }

    private void RotateLeftLine(
        BstNode<TKey, TValue> target,
        BstNode<TKey, TValue> ancestor,
        BstNode<TKey, TValue> greatAncestor)
    {
        RotateRight(greatAncestor);
        RotateRight(ancestor);
    }

    private void RotateRightLine(
        BstNode<TKey, TValue> target,
        BstNode<TKey, TValue> ancestor,
        BstNode<TKey, TValue> greatAncestor)
    {
        RotateLeft(greatAncestor);
        RotateLeft(ancestor);
    }

    private void RotateLeftRight(
        BstNode<TKey, TValue> target,
        BstNode<TKey, TValue> ancestor,
        BstNode<TKey, TValue> greatAncestor)
    {
        RotateLeft(ancestor);
        RotateRight(greatAncestor);
    }

    private void RotateRightLeft(
        BstNode<TKey, TValue> target,
        BstNode<TKey, TValue> ancestor,
        BstNode<TKey, TValue> greatAncestor)
    {
        RotateRight(ancestor);
        RotateLeft(greatAncestor);
    }
}
