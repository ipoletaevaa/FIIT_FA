using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(entry => entry.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(entry => entry.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        if (Root is null)
        {
            Root = CreateNode(key, value);
            Count++;
            OnNodeAdded(Root);
            return;
        }

        TNode current = Root;
        while (true)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                current.Value = value;
                return;
            }

            if (cmp < 0)
            {
                if (current.Left is null)
                {
                    TNode newNode = CreateNode(key, value);
                    current.Left = newNode;
                    newNode.Parent = current;
                    Count++;
                    OnNodeAdded(newNode);
                    return;
                }

                current = current.Left;
            }
            else
            {
                if (current.Right is null)
                {
                    TNode newNode = CreateNode(key, value);
                    current.Right = newNode;
                    newNode.Parent = current;
                    Count++;
                    OnNodeAdded(newNode);
                    return;
                }

                current = current.Right;
            }
        }
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        TNode? rebalanceParent;
        TNode? rebalanceChild;

        if (node.Left is null || node.Right is null)
        {
            rebalanceParent = node.Parent;
            rebalanceChild = node.Left ?? node.Right;
            Transplant(node, rebalanceChild);
            OnNodeRemoved(rebalanceParent, rebalanceChild);
            return;
        }

        TNode successor = Minimum(node.Right);

        if (successor.Parent != node)
        {
            rebalanceParent = successor.Parent;
            rebalanceChild = successor.Right;
            Transplant(successor, rebalanceChild);

            successor.Right = node.Right;
            if (successor.Right is not null)
            {
                successor.Right.Parent = successor;
            }
        }
        else
        {
            rebalanceParent = node.Parent;
            rebalanceChild = successor;
        }

        Transplant(node, successor);
        successor.Left = node.Left;
        if (successor.Left is not null)
        {
            successor.Left.Parent = successor;
        }

        OnNodeRemoved(rebalanceParent, rebalanceChild);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode node)
    {
        TNode? rightChild = node.Right;
        if (rightChild is null) return;

        TNode? parent = node.Parent;
        TNode? rightLeftChild = rightChild.Left;

        rightChild.Left = node;
        node.Parent = rightChild;

        node.Right = rightLeftChild;
        if (rightLeftChild is not null)
        {
            rightLeftChild.Parent = node;
        }

        if (parent is null)
        {
            Root = rightChild;
            rightChild.Parent = null;
        }
        else if (parent.Left == node)
        {
            parent.Left = rightChild;
            rightChild.Parent = parent;
        }
        else
        {
            parent.Right = rightChild;
            rightChild.Parent = parent;
        }
    }

    protected void RotateRight(TNode node)
    {
        TNode? leftChild = node.Left;
        if (leftChild is null) return;

        TNode? parent = node.Parent;
        TNode? leftRightChild = leftChild.Right;

        leftChild.Right = node;
        node.Parent = leftChild;

        node.Left = leftRightChild;
        if (leftRightChild is not null)
        {
            leftRightChild.Parent = node;
        }

        if (parent is null)
        {
            Root = leftChild;
            leftChild.Parent = null;
        }
        else if (parent.Left == node)
        {
            parent.Left = leftChild;
            leftChild.Parent = parent;
        }
        else
        {
            parent.Right = leftChild;
            leftChild.Parent = parent;
        }
    }
    
    protected void RotateBigLeft(TNode node)
    {
        TNode? rightChild = node.Right;
        if (rightChild is null) return;

        RotateRight(rightChild);
        RotateLeft(node);
    }
    
    protected void RotateBigRight(TNode node)
    {
        TNode? leftChild = node.Left;
        if (leftChild is null) return;

        RotateLeft(leftChild);
        RotateRight(node);
    }
    
    protected void RotateDoubleLeft(TNode node) => RotateBigLeft(node);
    
    protected void RotateDoubleRight(TNode node) => RotateBigRight(node);

    protected TNode Minimum(TNode node)
    {
        while (node.Left is not null)
        {
            node = node.Left;
        }

        return node;
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() =>
        new TreeIterator(Root, TraversalKind.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() =>
        new TreeIterator(Root, TraversalKind.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() =>
        new TreeIterator(Root, TraversalKind.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() =>
        new TreeIterator(Root, TraversalKind.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() =>
        new TreeIterator(Root, TraversalKind.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() =>
        new TreeIterator(Root, TraversalKind.PostOrderReverse);

    private enum TraversalKind
    {
        InOrder,
        PreOrder,
        PostOrder,
        InOrderReverse,
        PreOrderReverse,
        PostOrderReverse
    }

    private static int ComputeSubtreeHeight(TNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        return 1 + Math.Max(ComputeSubtreeHeight(node.Left), ComputeSubtreeHeight(node.Right));
    }

    private static void CollectTraversal(
        TNode? node,
        TraversalKind kind,
        List<TreeEntry<TKey, TValue>> entries)
    {
        if (node is null)
        {
            return;
        }

        bool visitBeforeLeft = kind is TraversalKind.PreOrder;
        bool visitBeforeRight = kind is TraversalKind.InOrder or TraversalKind.InOrderReverse;
        bool visitAfterChildren = kind is TraversalKind.PostOrder;
        bool reverseChildren = kind is TraversalKind.InOrderReverse;

        if (visitBeforeLeft)
        {
            entries.Add(new TreeEntry<TKey, TValue>(node.Key, node.Value, ComputeSubtreeHeight(node)));
        }

        TNode? firstChild = reverseChildren ? node.Right : node.Left;
        TNode? secondChild = reverseChildren ? node.Left : node.Right;

        CollectTraversal(firstChild, kind, entries);

        if (visitBeforeRight)
        {
            entries.Add(new TreeEntry<TKey, TValue>(node.Key, node.Value, ComputeSubtreeHeight(node)));
        }

        CollectTraversal(secondChild, kind, entries);

        if (visitAfterChildren)
        {
            entries.Add(new TreeEntry<TKey, TValue>(node.Key, node.Value, ComputeSubtreeHeight(node)));
        }
    }

    private static List<TreeEntry<TKey, TValue>> BuildTraversal(TNode? root, TraversalKind kind)
    {
        List<TreeEntry<TKey, TValue>> entries = new();
        TraversalKind collectKind = kind switch
        {
            TraversalKind.PreOrderReverse => TraversalKind.PreOrder,
            TraversalKind.PostOrderReverse => TraversalKind.PostOrder,
            _ => kind
        };

        CollectTraversal(root, collectKind, entries);

        if (kind is TraversalKind.PreOrderReverse or TraversalKind.PostOrderReverse)
        {
            entries.Reverse();
        }

        return entries;
    }

    /// <summary>
    /// Внутренний класс-итератор.
    /// Реализует паттерн Iterator вручную, без yield return.
    /// </summary>
    private sealed class TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly List<TreeEntry<TKey, TValue>> _entries;
        private int _index = -1;

        public TreeIterator(TNode? root, TraversalKind kind)
        {
            _entries = BuildTraversal(root, kind);
        }

        public TreeEntry<TKey, TValue> Current => _entries[_index];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < _entries.Count;
        }

        public void Reset() => _index = -1;

        public void Dispose()
        {
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class KeyValuePairEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly IEnumerator<TreeEntry<TKey, TValue>> _inner;

        public KeyValuePairEnumerator(IEnumerator<TreeEntry<TKey, TValue>> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public KeyValuePair<TKey, TValue> Current =>
            new(_inner.Current.Key, _inner.Current.Value);

        object IEnumerator.Current => Current;

        public bool MoveNext() => _inner.MoveNext();

        public void Reset() => _inner.Reset();

        public void Dispose() => _inner.Dispose();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        new KeyValuePairEnumerator(new TreeIterator(Root, TraversalKind.InOrder));

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Destination array is too small.", nameof(array));
        }

        TreeIterator iterator = new(Root, TraversalKind.InOrder);
        while (iterator.MoveNext())
        {
            TreeEntry<TKey, TValue> entry = iterator.Current;
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}