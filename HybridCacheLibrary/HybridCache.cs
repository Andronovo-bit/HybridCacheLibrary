using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HybridCacheLibrary
{
    public class HybridCache<K, V>
    {
        private readonly int _initialCapacity;
        private int _capacity;
        private readonly ConcurrentDictionary<K, Node<K, V>> _cache;
        private readonly ConcurrentDictionary<int, DoublyLinkedList<K, V>> _frequencyList;
        private int _minFrequency;
        private readonly NodePool<K, V> _nodePool;

        public HybridCache(int capacity)
        {
            _initialCapacity = capacity;
            _capacity = capacity;
            _cache = new ConcurrentDictionary<K, Node<K, V>>(capacity, capacity);
            _frequencyList = new ConcurrentDictionary<int, DoublyLinkedList<K, V>>();
            _minFrequency = 1;
            _nodePool = new NodePool<K, V>();
        }

        public V Get(K key)
        {
            if (!_cache.TryGetValue(key, out var node))
            {
                throw new KeyNotFoundException("The given key was not present in the cache.");
            }

            bool updateNeeded = false;

            // Optimistically check if we need to update the frequency
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
                    // Recheck the frequency in case it was updated in the meantime
                    if (node.Frequency == currentFrequency)
                    {
                        UpdateNodeFrequency(node);
                    }
                }
            }

            return node.Value;
        }


        public void Add(K key, V value)
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


        private void UpdateNodeFrequency(Node<K, V> node)
        {
            var oldFrequency = node.Frequency;
            var oldList = _frequencyList[oldFrequency];

            // Düğümü eski frekans listesinden çıkarıyoruz
            oldList.Remove(node);

            // Eğer bu, minimum frekanstaki son öğe ise, minFrequency'yi güncelle
            if (oldFrequency == _minFrequency && oldList.IsEmpty())
            {
                _minFrequency++;
            }

            // Frekansı artırıyoruz
            node.Frequency++;

            // Yeni frekans listesine ekliyoruz
            AddToFrequencyList(node);
        }


        private void AddToFrequencyList(Node<K, V> node)
        {
            // Eğer ilgili frekans için bir liste yoksa, yeni bir liste oluştur ve ekle
            if (!_frequencyList.TryGetValue(node.Frequency, out var list))
            {
                list = new DoublyLinkedList<K, V>();
                _frequencyList[node.Frequency] = list;
            }

            // Düğümü listenin başına ekliyoruz
            list.AddFirst(node);

            // Minimum frekansın güncellenmesi
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

            // Sonsuz döngüyü önlemek için güvenlik kontrolü ekliyoruz.
            int attempts = 0;
            int maxAttempts = _cache.Count - _capacity;
            while (_cache.Count > _capacity)
            {
                Evict();
                attempts++;

                // Eğer evict işlemi _cache.Count değerini düşürmüyorsa, döngüyü durdur
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

                    // Eğer liste boş kaldıysa, minimum frekansı güncelle
                    if (list.IsEmpty())
                    {
                        _frequencyList.TryRemove(_minFrequency, out _);
                        _minFrequency = _frequencyList.Count > 0 ? _frequencyList.Where(t => !t.Value.IsEmpty()).Min(t => t.Key) : 1;

                        _frequencyList.Where(t => !t.Value.IsEmpty()).ToList();
                    }
                }
            }
        }

    }
}
