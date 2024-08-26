using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Caching.Memory;
using static HybridCacheLibrary.Benchmark.HybridCacheBenchmark;

namespace HybridCacheLibrary.Benchmark
{
    [MemoryDiagnoser]
    public class HybridCacheBenchmark
    {
        [MemoryDiagnoser]
        public class HybridCacheVsMemoryCacheBenchmark
        {
            private CountBasedHybridCache<int, string> _hybridCacheCountBased;
            private SizeBasedHybridCache<int, string> _hybridCacheSizeBased;
            private MemoryCache _memoryCache;
            private readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            [Params(100, 1000, 10000)] // Different cache sizes
            public int CacheSize { get; set; }

            [GlobalSetup]
            public void Setup()
            {
                _hybridCacheCountBased = new CountBasedHybridCache<int, string>(CacheSize);
                _hybridCacheSizeBased = new SizeBasedHybridCache<int, string>(CacheSize, CacheSizeType.Megabytes); // Cache boyutunu Mbyte cinsinden belirliyoruz
                _memoryCache = new MemoryCache(new MemoryCacheOptions());

                for (int i = 0; i < CacheSize; i++)
                {
                    var str = "value" + i;
                    _hybridCacheCountBased.Add(i, str);
                    _hybridCacheSizeBased.Add(i, str);
                    _memoryCache.Set(i, str, _cacheEntryOptions);
                }
            }

            [Benchmark]
            public void AddItemsHybridCacheCountBased()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _hybridCacheCountBased.Add(i, "newvalue" + i);
                }
            }

            [Benchmark]
            public void AddItemsHybridCacheSizeBased()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _hybridCacheSizeBased.Add(i, "newvalue" + i);
                }
            }

            [Benchmark]
            public void AddItemsMemoryCache()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _memoryCache.Set(i, "newvalue" + i, _cacheEntryOptions);
                }
            }

            [Benchmark]
            public void GetItemsHybridCacheCountBased()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _hybridCacheCountBased.Get(i);
                }
            }

            [Benchmark]
            public void GetItemsHybridCacheSizeBased()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _hybridCacheSizeBased.Get(i);
                }
            }

            [Benchmark]
            public void GetItemsMemoryCache()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _memoryCache.TryGetValue(i, out _);
                }
            }

            [Benchmark]
            public void HybridCacheCountBased_LongRunning()
            {
                for (int i = 0; i < 1000000; i++)
                {
                    _hybridCacheCountBased.Add(i % CacheSize, $"Value {i}");
                    var value = _hybridCacheCountBased.Get(i % CacheSize);
                }
            }

            [Benchmark]
            public void HybridCacheSizeBased_LongRunning()
            {
                for (int i = 0; i < 1000000; i++)
                {
                    _hybridCacheSizeBased.Add(i % CacheSize, $"Value {i}");
                    var value = _hybridCacheSizeBased.Get(i % CacheSize);
                }
            }

            [Benchmark]
            public void MemoryCache_LongRunning()
            {
                for (int i = 0; i < 1000000; i++)
                {
                    _memoryCache.Set(i % CacheSize, $"Value {i}");
                    var value = _memoryCache.Get(i % CacheSize);
                }
            }

            [Benchmark]
            public void HybridCacheCountBased_Concurrent_Add_Get()
            {
                Parallel.For(0, CacheSize, i =>
                {
                    _hybridCacheCountBased.Add(i, $"Value {i}");
                    var value = _hybridCacheCountBased.Get(i);
                });
            }

            [Benchmark]
            public void HybridCacheSizeBased_Concurrent_Add_Get()
            {
                Parallel.For(0, CacheSize, i =>
                {
                    _hybridCacheSizeBased.Add(i, $"Value {i}");
                    var value = _hybridCacheSizeBased.Get(i);
                });
            }

            [Benchmark]
            public void MemoryCache_Concurrent_Add_Get()
            {
                Parallel.For(0, CacheSize, i =>
                {
                    _memoryCache.Set(i, $"Value {i}");
                    var value = _memoryCache.Get(i);
                });
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<HybridCacheVsMemoryCacheBenchmark>();
        }
    }
}
