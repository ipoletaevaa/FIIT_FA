using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new AvlNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        if (newNode is null) return;
        RebalancePath(newNode.Parent);
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        RebalancePath(parent);
    }

    private void RebalancePath(AvlNode<TKey, TValue>? node)
    {
        while (node is not null)
        {
            UpdateHeight(node);
            
            if (IsUnbalanced(node))
            {
                Balance(node);
            }
            
            node = node.Parent;
        }
    }

    private int GetHeight(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;

    private int GetBalanceFactor(AvlNode<TKey, TValue> node)
    {
        if (node is null) return 0;
        return GetHeight(node.Right) - GetHeight(node.Left);
    }

    private void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        if (node is null) return;
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }

    private bool IsUnbalanced(AvlNode<TKey, TValue> node)
    {
        if (node is null) return false;
        return Math.Abs(GetBalanceFactor(node)) > 1;
    }

    private void Balance(AvlNode<TKey, TValue> node)
    {
        if (node is null) return;

        int balanceFactor = GetBalanceFactor(node);

        if (balanceFactor > 1)
        {
            BalanceRightHeavy(node);
        }
        else if (balanceFactor < -1)
        {
            BalanceLeftHeavy(node);
        }
    }

    private void BalanceRightHeavy(AvlNode<TKey, TValue> node)
    {
        var rightChild = node.Right;
        
        if (rightChild is null) return;

        if (GetBalanceFactor(rightChild) < 0)
        {
            var rightLeftChild = rightChild.Left;
            RotateBigLeft(node);
            
            UpdateHeight(node);
            UpdateHeight(rightChild);
            
            if (rightLeftChild is not null)
            {
                UpdateHeight(rightLeftChild);
            }
        }
        else
        {
            RotateLeft(node);
            UpdateHeight(node);
            UpdateHeight(rightChild);
        }
    }

    private void BalanceLeftHeavy(AvlNode<TKey, TValue> node)
    {
        var leftChild = node.Left;
        
        if (leftChild is null) return;

        if (GetBalanceFactor(leftChild) > 0)
        {
            var leftRightChild = leftChild.Right;
            RotateBigRight(node);
            
            UpdateHeight(node);
            UpdateHeight(leftChild);
            
            if (leftRightChild is not null)
            {
                UpdateHeight(leftRightChild);
            }
        }
        else
        {
            RotateRight(node);
            UpdateHeight(node);
            UpdateHeight(leftChild);
        }
    }

}