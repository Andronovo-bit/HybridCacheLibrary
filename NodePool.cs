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
                node.Key = key;
                node.Value = value;
                node.Frequency = 1;
                node.Prev = null;
                node.Next = null;
                return node;
            }
            return new Node<K, V>(key, value);
        }

        public void Return(Node<K, V> node)
        {
            node.Key = default;
            node.Value = default;
            node.Frequency = 0;
            node.Prev = null;
            node.Next = null;
            _pool.Add(node);
        }
    }
}
