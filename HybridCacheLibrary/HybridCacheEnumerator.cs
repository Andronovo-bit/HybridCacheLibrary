using System.Collections;
using System.Collections.Generic;

namespace HybridCacheLibrary
{
    internal class HybridCacheEnumerator<K, V> : IEnumerator<KeyValuePair<K, V>>
    {
        private readonly IEnumerator<KeyValuePair<K, Node<K, V>>> _cacheEnumerator;

        public HybridCacheEnumerator(HybridCache<K, V> hybridCache)
        {
            _cacheEnumerator = hybridCache.GetCacheEnumerator();
        }

        public KeyValuePair<K, V> Current => new KeyValuePair<K, V>(_cacheEnumerator.Current.Key, _cacheEnumerator.Current.Value.Value);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _cacheEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            return _cacheEnumerator.MoveNext();
        }

        public void Reset()
        {
            _cacheEnumerator.Reset();
        }
    }
}
