
# HybridCacheLibrary

## Overview

**HybridCacheLibrary** is a custom caching library implemented in C#. This library provides a hybrid caching mechanism that combines the benefits of both Least Recently Used (LRU) and Least Frequently Used (LFU) cache eviction policies. The library is optimized for performance, ensuring fast add and retrieval operations while managing cache capacity dynamically. It also includes thread safety mechanisms, making it suitable for concurrent environments.

## Features

- **Hybrid Caching Mechanism**: Combines LRU and LFU strategies for efficient cache eviction.
- **Thread-Safe Operations**: Built-in thread safety to handle concurrent add and get operations.
- **Dynamic Capacity Management**: Supports dynamic cache resizing with an option to shrink the cache size.
- **Customizable Frequency Settings**: Allows setting custom frequency values for cache items to prioritize eviction.
- **Object Pooling**: Utilizes object pooling to optimize memory usage and improve performance.
- **Benchmarking**: Includes benchmark tests to compare performance against `MemoryCache`.

## Project Structure

```plaintext
HybridCacheLibrary/
│
├── HybridCacheLibrary/
│   ├── HybridCache.cs
│   ├── DoublyLinkedList.cs
│   ├── HybridCacheEnumerator.cs
│   ├── Node.cs
│   └── NodePool.cs
│
├── HybridCacheLibrary.Tests/
│   ├── HybridCacheTests.cs
│   └── RealWorldTests.cs
│
└── HybridCacheLibrary.Benchmark/
    └── Program.cs
```

## Installation

To use **HybridCacheLibrary** in your project, you can include the library as a reference in your `.NET` project.


```bash
git clone https://github.com/Andronovo-bit/HybridCacheLibrary.git
```

## Usage

### Basic Usage

```csharp
using HybridCacheLibrary;

var cache = new HybridCache<int, string>(100);

// Adding items
cache.Add(1, "Value1");
cache.Add(2, "Value2", 5); // Add with custom frequency

// Retrieving items
var value = cache.Get(1);
Console.WriteLine(value); // Output: Value1

// Checking frequency
int frequency = cache.GetFrequency(1);
Console.WriteLine(frequency); // Output: 2

// Eviction
cache.SetCapacity(50, shrink: true);
```

### Benchmarking

Benchmark tests have been provided to compare the performance of `HybridCache` against `MemoryCache`. These can be run using the `BenchmarkDotNet` library.

#### Results and Analysis

Below is a benchmark comparison between `HybridCache` and `MemoryCache` across various operations such as adding items, getting items, long-running operations, and concurrent add/get operations. The tests were conducted with cache sizes of 100, 1000, and 10,000.

![Benchmark Results](HybridCacheLibrary.Benchmark/benchmark_result.png)

**Key Observations:**

- **Add Operations**: 
  - `HybridCache` shows consistently faster add operations than `MemoryCache`, particularly at smaller cache sizes. This is likely due to the efficient use of internal data structures and object pooling.
  - At a cache size of 10,000, the difference is more pronounced, with `HybridCache` taking `468.137 us` versus `1,113.104 us` for `MemoryCache`.

- **Get Operations**: 
  - `HybridCache` also demonstrates superior performance in get operations across all cache sizes. For example, at a cache size of 10,000, `HybridCache` achieves `104.457 us` compared to `348.644 us` for `MemoryCache`.
  - This indicates that `HybridCache` is optimized for fast retrievals, especially in larger caches.

- **Long-Running Operations**: 
  - `HybridCache` outperforms `MemoryCache` in long-running operations, particularly in managing frequent adds and gets. At a cache size of 100, `HybridCache` completes the task in `92,716.286 us` while `MemoryCache` takes `127,336.915 us`.
  - The advantage is maintained as the cache size increases, demonstrating better scalability under load.

- **Concurrent Add/Get Operations**: 
  - While `MemoryCache` performs slightly better in concurrent scenarios, with `HybridCache` showing `30.281 us` for a cache size of 100 compared to `13.235 us` for `MemoryCache`, the difference narrows as the cache size increases.
  - This indicates that while `HybridCache` is generally efficient, there may be room for optimization in handling high-concurrency scenarios.

### Testing

Unit tests are included to ensure the functionality and correctness of the library. You can run the tests using the following command:

```bash
dotnet test
```

## How It Works

### Hybrid Caching Mechanism

**HybridCacheLibrary** employs a hybrid approach combining both Least Recently Used (LRU) and Least Frequently Used (LFU) strategies to manage cache eviction. Here's how it works:

1. **Adding Items**: 
   - When an item is added to the cache, it is associated with a frequency counter, starting at 1 or a custom value if specified.
   - The item is placed in the corresponding frequency list within the cache.

2. **Retrieving Items**:
   - When an item is retrieved from the cache, its frequency counter is incremented.
   - The item's position is updated within the frequency list to reflect its new frequency.

3. **Eviction Policy**:
   - If the cache reaches its capacity, the item with the lowest frequency is considered for eviction.
   - Among the items with the same frequency, the Least Recently Used (LRU) item is evicted first.
   - This dual strategy ensures that items frequently accessed remain in the cache, while those accessed less frequently and least recently are evicted.

4. **Thread Safety**:
   - The cache operations are thread-safe, utilizing locking mechanisms to ensure data consistency during concurrent access.
   - A `ThreadLocal` cache is also used to optimize performance by reducing contention on shared resources.

5. **Dynamic Capacity Management**:
   - The cache size can be adjusted dynamically. When shrinking the cache, the least important items (based on frequency and recency) are evicted first.

6. **Object Pooling**:
   - The `NodePool` class handles the creation and recycling of cache nodes to optimize memory usage and minimize garbage collection overhead.

### Example Scenario

Consider a scenario where a cache is used to store frequently accessed data. The hybrid approach ensures that:
- Data frequently accessed will have higher frequencies and thus are less likely to be evicted.
- Data accessed infrequently but recently is also considered, providing a balanced approach between recency and frequency.

This makes **HybridCacheLibrary** particularly useful in applications where access patterns can vary, and both frequency and recency of access are important considerations.

## Acknowledgments

This project was inspired by the article [Implementing an LRU Cache in C#: A Step-by-Step Guide](https://medium.com/@caglarcansarikaya/implementing-an-lru-cache-in-c-a-step-by-step-guide-1cfa4b5d5512) by **Çağlar Can Sarıkaya**. His work provided valuable insights into the implementation of caching mechanisms, which influenced the development of this library.

## Contributing

Contributions are welcome! If you find a bug or have a feature request, please open an issue or submit a pull request. When contributing code, please ensure that it adheres to the coding standards and is well-documented.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contact

If you have any questions or feedback, feel free to reach out to me via [email](mailto:seyyid364@gmail.com) or [GitHub](https://github.com/Andronovo-bit).
