using System.Collections.Concurrent;

namespace HybridCacheLibrary
{
    internal class NodePool<K, V>
    {
        private readonly ConcurrentBag<Node<K, V>> _pool = new ConcurrentBag<Node<K, V>>();

        public Node<K, V> Get(K key, V value)
        {
            if (_pool.TryTake(out var node))
            {
                InitializeNode(node, key, value);
                return node;
            }
            return new Node<K, V>(key, value);
        }

        public void Return(Node<K, V> node)
        {
            if (node != null)
            {
                ResetNode(node);
                _pool.Add(node);
            }
        }

        private void InitializeNode(Node<K, V> node, K key, V value)
        {
            node.Key = key;
            node.Value = value;
            node.Frequency = 1;
            node.Prev = null;
            node.Next = null;
        }

        private void ResetNode(Node<K, V> node)
        {
            node.Key = default;
            node.Value = default;
            node.Frequency = 0;
            node.Prev = null;
            node.Next = null;
        }
    }
}
