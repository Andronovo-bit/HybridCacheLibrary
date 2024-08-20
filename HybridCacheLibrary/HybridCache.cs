using System.Collections;
using System.Collections.Concurrent;

namespace HybridCacheLibrary
{
    public class HybridCache<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        private readonly int _initialCapacity;
        private int _capacity;
        private readonly ConcurrentDictionary<K, Node<K, V>> _cache;
        private readonly ConcurrentDictionary<int, DoublyLinkedList<K, V>> _frequencyList;
        private int _minFrequency;
        private readonly NodePool<K, V> _nodePool;
        private readonly object _lockObject = new object();
        private readonly ThreadLocal<Dictionary<K, V>> _threadLocalCache = new ThreadLocal<Dictionary<K, V>>(() => new Dictionary<K, V>());

        public int Capacity => _capacity;

        public HybridCache(int capacity)
        {
            _initialCapacity = capacity;
            _capacity = capacity;
            _cache = new ConcurrentDictionary<K, Node<K, V>>(capacity, capacity);
            _frequencyList = new ConcurrentDictionary<int, DoublyLinkedList<K, V>>();
            _minFrequency = 1;
            _nodePool = new NodePool<K, V>();
        }

        internal IEnumerator<KeyValuePair<K, Node<K, V>>> GetCacheEnumerator()
        {
            return _cache.GetEnumerator();
        }


        public V Get(K key)
        {
            var localCache = _threadLocalCache.Value;

            if (localCache.TryGetValue(key, out var localValue))
            {
                return localValue;
            }

            if (!_cache.TryGetValue(key, out var node))
            {
                throw new KeyNotFoundException("The given key was not present in the cache.");
            }

            lock (node)
            {
                UpdateNodeFrequency(node);
            }

            localCache[key] = node.Value;
            return node.Value;
        }

        public bool TryGet(K key, out V value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                value = node.Value;  // Directly accessing node's value without calling Get to avoid double lock
                lock (node)
                {
                    UpdateNodeFrequency(node);
                }
                return true;
            }
            value = default;
            return false;
        }

        public int GetFrequency(K key)
        {
            if (!_cache.TryGetValue(key, out var node))
            {
                throw new KeyNotFoundException("The given key was not present in the cache.");
            }
            return node.Frequency;
        }

        public void Add(K key, V value)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    lock (existingNode)
                    {
                        if (!EqualityComparer<V>.Default.Equals(existingNode.Value, value))
                        {
                            existingNode.Value = value;
                            UpdateNodeFrequency(existingNode);
                        }
                    }
                    return;
                }

                if (_cache.Count >= _capacity)
                {
                    Evict();
                }

                var newNode = _nodePool.Get(key, value);
                _cache[key] = newNode;
                AddToFrequencyList(newNode);
            }
        }

        private void AddToFrequencyList(Node<K, V> node)
        {
            if (!_frequencyList.TryGetValue(node.Frequency, out var list))
            {
                list = new DoublyLinkedList<K, V>();
                _frequencyList[node.Frequency] = list;
            }

            list.AddFirst(node);

            if (node.Frequency == 1 || node.Frequency < _minFrequency)
            {
                _minFrequency = node.Frequency;
            }
        }

        private void UpdateNodeFrequency(Node<K, V> node)
        {
            var oldFrequency = node.Frequency;
            if (_frequencyList.TryGetValue(oldFrequency, out var oldList))
            {
                oldList.Remove(node);

                if (oldFrequency == _minFrequency && oldList.IsEmpty())
                {
                    _minFrequency++;
                    _frequencyList.TryRemove(oldFrequency, out _);
                }
            }

            node.Frequency++;
            AddToFrequencyList(node);
        }

        public void SetCapacity(int newCapacity, bool shrink = false)
        {
            lock (_lockObject)
            {
                if (newCapacity < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(newCapacity), "Capacity must be greater than zero.");
                }

                if (newCapacity == _capacity)
                {
                    return;
                }

                _capacity = Math.Max(_initialCapacity, newCapacity);

                if (shrink)
                {
                    _capacity = newCapacity;
                }

                int itemsToRemove = Math.Max(0, _cache.Count - _capacity);
                for (int i = 0; i < itemsToRemove; i++)
                {
                    Evict();
                }
            }
        }

        private void Evict()
        {
            if (_frequencyList.TryGetValue(_minFrequency, out var list))
            {
                var nodeToEvict = list.RemoveLast();
                if (nodeToEvict != null)
                {
                    if (_cache.TryRemove(nodeToEvict.Key, out _))
                    {
                        _nodePool.Return(nodeToEvict);
                    }

                    if (list.IsEmpty())
                    {
                        _frequencyList.TryRemove(_minFrequency, out _);
                        UpdateMinFrequency();
                    }
                }
            }
        }

        private void UpdateMinFrequency()
        {
            foreach (var key in _frequencyList.Keys)
            {
                if (!_frequencyList[key].IsEmpty())
                {
                    _minFrequency = key;
                    return;
                }
            }
            _minFrequency = 1;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return new HybridCacheEnumerator<K, V>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
