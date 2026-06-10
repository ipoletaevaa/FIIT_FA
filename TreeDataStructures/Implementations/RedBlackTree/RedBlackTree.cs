using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new RbNode<TKey, TValue>(key, value) { Color = RbColor.Red };
    }

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        BalanceAfterInsert(newNode);
        Root!.Color = RbColor.Black;
    }

    protected override void RemoveNode(RbNode<TKey, TValue> target)
    {
        if (target.Left is null || target.Right is null)
        {
            DeleteNodeWithAtMostOneChild(target);
            return;
        }

        DeleteNodeWithTwoChildren(target);
    }

    private void DeleteNodeWithAtMostOneChild(RbNode<TKey, TValue> target)
    {
        RbColor detachedColor = target.Color;
        RbNode<TKey, TValue>? child = target.Left ?? target.Right;
        RbNode<TKey, TValue>? parent = target.Parent;

        Transplant(target, child);

        if (detachedColor == RbColor.Black)
        {
            RestoreBalanceAfterDelete(child, parent);
        }
    }

    private void DeleteNodeWithTwoChildren(RbNode<TKey, TValue> target)
    {
        RbNode<TKey, TValue> rightSubtree = target.Right!;
        RbNode<TKey, TValue> successor = Minimum(rightSubtree);

        if (successor.Parent != target)
        {
            RemoveInternalSuccessor(successor, target);
            return;
        }

        RemoveDirectSuccessor(successor, target);
    }

    private void RemoveInternalSuccessor(RbNode<TKey, TValue> successor, RbNode<TKey, TValue> target)
    {
        RbColor detachedColor = successor.Color;
        RbNode<TKey, TValue>? child = successor.Right;
        RbNode<TKey, TValue>? parent = successor.Parent;

        Transplant(successor, child);

        successor.Right = target.Right;
        if (successor.Right is not null)
        {
            successor.Right.Parent = successor;
        }

        Transplant(target, successor);
        successor.Left = target.Left;
        if (successor.Left is not null)
        {
            successor.Left.Parent = successor;
        }

        successor.Color = target.Color;

        if (detachedColor == RbColor.Black)
        {
            RestoreBalanceAfterDelete(child, parent);
        }
    }

    private void RemoveDirectSuccessor(RbNode<TKey, TValue> successor, RbNode<TKey, TValue> target)
    {
        RbColor detachedColor = successor.Color;
        RbNode<TKey, TValue>? child = successor.Right;

        Transplant(target, successor);
        successor.Left = target.Left;
        if (successor.Left is not null)
        {
            successor.Left.Parent = successor;
        }

        successor.Color = target.Color;

        if (detachedColor == RbColor.Black)
        {
            RestoreBalanceAfterDelete(child, successor);
        }
    }

    private void BalanceAfterInsert(RbNode<TKey, TValue> inserted)
    {
        RbNode<TKey, TValue> current = inserted;

        while (current.Parent is not null && current.Parent.Color == RbColor.Red)
        {
            RbNode<TKey, TValue> parent = current.Parent;
            RbNode<TKey, TValue> grandparent = parent.Parent!;

            if (parent == grandparent.Left)
            {
                current = ResolveLeftSubtreeViolation(current, parent, grandparent);
            }
            else
            {
                current = ResolveRightSubtreeViolation(current, parent, grandparent);
            }
        }
    }

    private RbNode<TKey, TValue> ResolveLeftSubtreeViolation(
        RbNode<TKey, TValue> current,
        RbNode<TKey, TValue> parent,
        RbNode<TKey, TValue> grandparent)
    {
        RbNode<TKey, TValue>? opposite = grandparent.Right;

        if (opposite is not null && opposite.Color == RbColor.Red)
        {
            RecolorRedRelatives(parent, opposite, grandparent);
            return grandparent;
        }

        if (current == parent.Right)
        {
            current = parent;
            RotateLeft(current);
            parent = current.Parent!;
        }

        parent.Color = RbColor.Black;
        grandparent.Color = RbColor.Red;
        RotateRight(grandparent);
        return current;
    }

    private RbNode<TKey, TValue> ResolveRightSubtreeViolation(
        RbNode<TKey, TValue> current,
        RbNode<TKey, TValue> parent,
        RbNode<TKey, TValue> grandparent)
    {
        RbNode<TKey, TValue>? opposite = grandparent.Left;

        if (opposite is not null && opposite.Color == RbColor.Red)
        {
            RecolorRedRelatives(parent, opposite, grandparent);
            return grandparent;
        }

        if (current == parent.Left)
        {
            current = parent;
            RotateRight(current);
            parent = current.Parent!;
        }

        parent.Color = RbColor.Black;
        grandparent.Color = RbColor.Red;
        RotateLeft(grandparent);
        return current;
    }

    private static void RecolorRedRelatives(
        RbNode<TKey, TValue> parent,
        RbNode<TKey, TValue> opposite,
        RbNode<TKey, TValue> grandparent)
    {
        parent.Color = RbColor.Black;
        opposite.Color = RbColor.Black;
        grandparent.Color = RbColor.Red;
    }

    private void RestoreBalanceAfterDelete(RbNode<TKey, TValue>? deficient, RbNode<TKey, TValue>? parent)
    {
        RbNode<TKey, TValue>? current = deficient;
        RbNode<TKey, TValue>? currentParent = parent;

        while (current != Root && (current is null || current.Color == RbColor.Black))
        {
            if (current == currentParent?.Left)
            {
                (current, currentParent) = FixDeficitOnLeftBranch(current, currentParent);
            }
            else
            {
                (current, currentParent) = FixDeficitOnRightBranch(current, currentParent);
            }
        }

        if (current is not null)
        {
            current.Color = RbColor.Black;
        }
    }

    private (RbNode<TKey, TValue>? Node, RbNode<TKey, TValue>? Parent) FixDeficitOnLeftBranch(
        RbNode<TKey, TValue>? deficient,
        RbNode<TKey, TValue>? parent)
    {
        RbNode<TKey, TValue>? adjacent = parent?.Right;

        if (adjacent is not null && adjacent.Color == RbColor.Red)
        {
            adjacent.Color = RbColor.Black;
            parent!.Color = RbColor.Red;
            RotateLeft(parent);
            adjacent = parent.Right;
        }

        if (BothChildrenBlack(adjacent))
        {
            if (adjacent is not null)
            {
                adjacent.Color = RbColor.Red;
            }

            return (parent, parent?.Parent);
        }

        if (adjacent?.Right is null || adjacent.Right.Color == RbColor.Black)
        {
            if (adjacent?.Left is not null)
            {
                adjacent.Left.Color = RbColor.Black;
            }

            if (adjacent is not null)
            {
                adjacent.Color = RbColor.Red;
                RotateRight(adjacent);
            }

            adjacent = parent?.Right;
        }

        if (adjacent is not null)
        {
            adjacent.Color = parent!.Color;
            if (adjacent.Right is not null)
            {
                adjacent.Right.Color = RbColor.Black;
            }
        }

        parent!.Color = RbColor.Black;
        RotateLeft(parent);
        return (Root, null);
    }

    private (RbNode<TKey, TValue>? Node, RbNode<TKey, TValue>? Parent) FixDeficitOnRightBranch(
        RbNode<TKey, TValue>? deficient,
        RbNode<TKey, TValue>? parent)
    {
        RbNode<TKey, TValue>? adjacent = parent?.Left;

        if (adjacent is not null && adjacent.Color == RbColor.Red)
        {
            adjacent.Color = RbColor.Black;
            parent!.Color = RbColor.Red;
            RotateRight(parent);
            adjacent = parent.Left;
        }

        if (BothChildrenBlack(adjacent))
        {
            if (adjacent is not null)
            {
                adjacent.Color = RbColor.Red;
            }

            return (parent, parent?.Parent);
        }

        if (adjacent?.Left is null || adjacent.Left.Color == RbColor.Black)
        {
            if (adjacent?.Right is not null)
            {
                adjacent.Right.Color = RbColor.Black;
            }

            if (adjacent is not null)
            {
                adjacent.Color = RbColor.Red;
                RotateLeft(adjacent);
            }

            adjacent = parent?.Left;
        }

        if (adjacent is not null)
        {
            adjacent.Color = parent!.Color;
            if (adjacent.Left is not null)
            {
                adjacent.Left.Color = RbColor.Black;
            }
        }

        if (parent is not null)
        {
            parent.Color = RbColor.Black;
            RotateRight(parent);
        }

        return (Root, null);
    }

    private static bool BothChildrenBlack(RbNode<TKey, TValue>? node)
    {
        return (node?.Left is null || node.Left.Color == RbColor.Black)
            && (node?.Right is null || node.Right.Color == RbColor.Black);
    }
}
