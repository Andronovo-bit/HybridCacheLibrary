using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace HybridCacheLibrary.Benchmark
{
    [MemoryDiagnoser]
    public class HybridCacheBenchmark
    {
        private HybridCache<int, string> _cache;

        [Params(100, 1000, 10000)] // Different cache sizes
        public int CacheSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _cache = new HybridCache<int, string>(CacheSize);
            for (int i = 0; i < CacheSize; i++)
            {
                _cache.Add(i, "value" + i);
            }
        }

        [Benchmark]
        public void AddItems()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                _cache.Add(i, "newvalue" + i);
            }
        }

        [Benchmark]
        public void GetItems()
        {
            for (int i = 0; i < CacheSize; i++)
            {
                _cache.Get(i);
            }
        }

        [Benchmark]
        public void SetCapacityAndEvict()
        {
            _cache.SetCapacity(CacheSize / 2);
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var summary = BenchmarkRunner.Run<HybridCacheBenchmark>();

        }
    }
}
