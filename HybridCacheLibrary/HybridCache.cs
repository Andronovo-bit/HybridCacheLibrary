using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

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
            lock (node)
            {
                UpdateNodeFrequency(node);
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
            lock (this) // Kilitleme mekanizması ekliyoruz
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    bool needsUpdate = false;
                    lock (existingNode)
                    {
                        if (!EqualityComparer<V>.Default.Equals(existingNode.Value, value))
                        {
                            existingNode.Value = value;
                            needsUpdate = true;
                        }
                    }
                    if (needsUpdate)
                    {
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
        private void AddToFrequencyList(Node<K, V> node)
        {
            // Eğer frekans listesi mevcut değilse, boş olan bir listeyi kullanmak yerine yeni bir tane oluşturuyoruz
            if (!_frequencyList.TryGetValue(node.Frequency, out var list))
            {
                // Boş olan listeleri yeniden kullanmak
                list = _frequencyList.FirstOrDefault(kv => kv.Value.IsEmpty()).Value ?? new DoublyLinkedList<K, V>();
                _frequencyList[node.Frequency] = list;
            }

            list.AddFirst(node);

            // Minimum frekansı sadece 1 ise veya mevcut frekanstan küçükse güncelleyin
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
                    // Liste boşsa, içeriği başka bir dolu listeyle değiştirmek yerine kaldırıyoruz
                    _minFrequency++;
                    _frequencyList.TryRemove(oldFrequency, out _);
                }
            }

            node.Frequency++;
            AddToFrequencyList(node);
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
                        UpdateMinFrequency();
                    }
                }
            }
        }

        private void UpdateMinFrequency()
        {
            _minFrequency = _frequencyList.Keys.Where(key => !_frequencyList[key].IsEmpty()).DefaultIfEmpty(1).Min();
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
