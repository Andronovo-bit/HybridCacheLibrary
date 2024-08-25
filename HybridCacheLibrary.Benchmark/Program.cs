using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using static HybridCacheLibrary.Benchmark.HybridCacheBenchmark;

namespace HybridCacheLibrary.Benchmark
{
    [MemoryDiagnoser]
    public class HybridCacheBenchmark
    {
        [MemoryDiagnoser]
        public class HybridCacheVsMemoryCacheBenchmark
        {
            private CountBasedHybridCache<int, string> _hybridCache;
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
                _hybridCache = new CountBasedHybridCache<int, string>(CacheSize);
                _memoryCache = new MemoryCache(new MemoryCacheOptions());

                for (int i = 0; i < CacheSize; i++)
                {
                    _hybridCache.Add(i, "value" + i);
                    _memoryCache.Set(i, "value" + i, _cacheEntryOptions);
                }
            }

            [Benchmark]
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

            [Benchmark]
            public void HybridCache_LongRunning()
            {
                for (int i = 0; i < 1000000; i++)
                {
                    _hybridCache.Add(i % CacheSize, $"Value {i}");
                    var value = _hybridCache.Get(i % CacheSize);
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
            public void HybridCache_Concurrent_Add_Get()
            {
                Parallel.For(0, CacheSize, i =>
                {
                    _hybridCache.Add(i, $"Value {i}");
                    var value = _hybridCache.Get(i);
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


            [Benchmark]
            public void AddItemsMemoryCache()
            {
                for (int i = 0; i < CacheSize; i++)
                {
                    _memoryCache.Set(i, "newvalue" + i, _cacheEntryOptions);
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
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            long beforeMemory = GC.GetTotalMemory(true);

            var user = new User(1, "John Doe", 30, new Address("Istanbul", "Turkey"));

            long afterMemory = GC.GetTotalMemory(true);

            Console.WriteLine($"Memory used: {afterMemory - beforeMemory} bytes");
            Console.WriteLine("Hello, World!");
            //var summary = BenchmarkRunner.Run<HybridCacheVsMemoryCacheBenchmark>();

        }
    }

    internal class User
    {
        public User(int id, string name, int age, Address address)
        {
            Id = id;
            Name = name;
            Age = age;
            Address = address;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public Address Address { get; set; }

        public int[] ints { get; set; }

        public List<string> strings { get; set; }
    }
    internal record Address(string City, string Country);
}
