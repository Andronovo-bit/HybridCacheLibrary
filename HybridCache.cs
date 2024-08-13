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

            lock (node)
            {
                UpdateNodeFrequency(node);
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

        public void SetCapacity(int newCapacity)
        {
            _capacity = Math.Max(_initialCapacity, newCapacity);
            while (_cache.Count > _capacity)
            {
                Evict();
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
            _frequencyList.GetOrAdd(node.Frequency, _ => new DoublyLinkedList<K, V>()).AddFirst(node);
            if (node.Frequency == 1 || node.Frequency < _minFrequency)
            {
                _minFrequency = node.Frequency;
            }
        }

        private void Evict()
        {
            if (_frequencyList.TryGetValue(_minFrequency, out var list))
            {
                var nodeToEvict = list.RemoveLast();
                if (nodeToEvict != null)
                {
                    _cache.TryRemove(nodeToEvict.Key, out _);
                    _nodePool.Return(nodeToEvict);
                }
            }
        }
    }
}
