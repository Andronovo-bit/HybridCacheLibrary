using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

        public int Capacity
        {
            get { return _capacity; }
        }


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
            if (!_cache.TryGetValue(key, out var node))
            {
                throw new KeyNotFoundException("The given key was not present in the cache.");
            }

            bool updateNeeded = false;

            var currentFrequency = node.Frequency;
            var newFrequency = currentFrequency + 1;
            if (!_frequencyList.TryGetValue(newFrequency, out _) || (node.Frequency != newFrequency))
            {
                updateNeeded = true;
            }

            if (updateNeeded)
            {
                lock (node)
                {
                    if (node.Frequency == currentFrequency)
                    {
                        UpdateNodeFrequency(node);
                    }
                }
            }

            return node.Value;
        }
        public bool TryGet(K key, out V value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                value = Get(key); // Get method updates frequency and returns the value
                return true;
            }
            value = default;
            return false;
        }

        public void Add(K key, V value)
        {
            lock (this) // Kilitleme mekanizması ekliyoruz
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    lock (existingNode)
                    {
                        existingNode.Value = value;
                        UpdateNodeFrequency(existingNode);
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

        private void UpdateNodeFrequency(Node<K, V> node)
        {
            var oldFrequency = node.Frequency;
            var oldList = _frequencyList[oldFrequency];
            oldList.Remove(node);

            if (oldFrequency == _minFrequency && oldList.IsEmpty())
            {
                _minFrequency++;
            }

            node.Frequency++;
            AddToFrequencyList(node);
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

        public void SetCapacity(int newCapacity, bool shrink = false)
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

            int attempts = 0;
            int maxAttempts = _cache.Count - _capacity;
            while (_cache.Count > _capacity)
            {
                Evict();
                attempts++;

                if (attempts > maxAttempts)
                {
                    break;
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
                        _minFrequency = _frequencyList.Count > 0 ? _frequencyList.Where(t => !t.Value.IsEmpty()).Min(t => t.Key) : 1;
                    }
                }
            }
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
