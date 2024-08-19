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
            private HybridCache<int, string> _hybridCache;
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
                _hybridCache = new HybridCache<int, string>(CacheSize);
                //_memoryCache = new MemoryCache(new MemoryCacheOptions());

                for (int i = 0; i < CacheSize; i++)
                {
                    _hybridCache.Add(i, "value" + i);
                    //_memoryCache.Set(i, "value" + i, _cacheEntryOptions);
                }
            }

            //[Benchmark]
            public void AddItemsHybridCache()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _hybridCache.Add(i, "newvalue" + i);
                }
            }

            [Benchmark]
            public void GetItemsHybridCache()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _hybridCache.Get(i);
                }
            }

            //[Benchmark]
            public void SetCapacityAndEvictHybridCache()
            {
                _hybridCache.SetCapacity(CacheSize / 2);
            }

            //[Benchmark]
            public void AddItemsMemoryCache()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _memoryCache.Set(i, "newvalue" + i, _cacheEntryOptions);
                }
            }

           // [Benchmark]
            public void GetItemsMemoryCache()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _memoryCache.TryGetValue(i, out _);
                }
            }
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var summary = BenchmarkRunner.Run<HybridCacheVsMemoryCacheBenchmark>();

            //Manual Test
            /*
            var cache = new HybridCache<int, string>(1000);


            for (int i = 0; i < 1000; i++)
            {
                cache.Add(i, "value" + i);
            }

            for (int i = 0; i < 1000; i++)
            {
                cache.Get(i);
            }

            Console.WriteLine($" Part One Elapsed Time: {cache.partOneTotalWatch} ms");
            Console.WriteLine($" Part Two Elapsed Time: {cache.partTwoTotalWatch} ms");
            Console.WriteLine($" Part Three Elapsed Time: {cache.partThreeTotalWatch} ms");
            */
            
        }
    }
}
