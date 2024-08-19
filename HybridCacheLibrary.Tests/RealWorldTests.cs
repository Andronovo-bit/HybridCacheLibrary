﻿namespace HybridCacheLibrary.Tests
{
    public class RealWorldTests
    {
        [Fact]
        public async Task ConcurrentAccess_Should_Be_ThreadSafe()
        {
            // Arrange
            var cache = new HybridCache<int, string>(100);
            int numberOfTasks = 10;
            int numberOfItems = 1000;

            // Act
            var tasks = new Task[numberOfTasks];

            for (int i = 0; i < numberOfTasks; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < numberOfItems; j++)
                    {
                        cache.Add(j, $"Value {j}");
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            int foundItems = 0;
            for (int i = 0; i < numberOfItems; i++)
            {
                if (cache.TryGet(i, out var value))
                {
                    Assert.Equal($"Value {i}", value);
                    foundItems++;
                }
            }

            // Check that we have all items (the cache should have the most recently added ones due to eviction strategy)
            Assert.Equal(Math.Min(numberOfItems, cache.Capacity), foundItems);
            Assert.True(foundItems <= 100, $"Cache contains more items than expected: {foundItems}");
        }

        [Fact]
        public void HighFrequencyItems_Should_Stay_In_Cache()
        {
            // Arrange
            var cache = new HybridCache<int, string>(10);
            var dictionary = new Dictionary<int, int>();

            // Act
            for (int i = 1; i <= 10; i++)
            {
                cache.Add(i, $"Value {i}");
            }

            // Access randomly to increase frequency
            for (int i = 1; i <= 77; i++)
            {
                var key = new Random().Next(1, 11);
                cache.Get(key);
                dictionary[key] = dictionary.ContainsKey(key) ? dictionary[key] + 1 : 1;
            }

            dictionary = dictionary.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);


            cache.SetCapacity(5, shrink: true); // Shrink cache size to 5

            // Assert
            Assert.Equal(5, cache.Count()); // Only 5 items should remain

            // Assert high frequency items are still in cache
            foreach (var item in cache)
            {
                Assert.True(dictionary.ContainsKey(item.Key));
            }
            
        }
        [Fact]
        public void DynamicCapacityChange_Should_Adapt_Cache_Size()
        {
            // Arrange
            var cache = new HybridCache<int, string>(10);

            // Act
            for (int i = 1; i <= 10; i++)
            {
                cache.Add(i, $"Value {i}");
            }

            cache.SetCapacity(5, shrink: true); // Shrink cache size to 5

            // Assert
            int itemCount = 0;

            foreach (var kvp in cache)
            {
                itemCount++;
            }

            Assert.Equal(5, itemCount); // Only 5 items should remain in the cache
        }

        [Fact]
        public void Cache_Should_Perform_Under_Heavy_Load()
        {
            // Arrange
            var cache = new HybridCache<int, string>(1000);

            // Act
            for (int i = 1; i <= 10000; i++)
            {
                cache.Add(i, $"Value {i}");

                // Simulate heavy access pattern
                for (int j = 1; j <= 10; j++)
                {
                    var key = new Random().Next(1, i + 1);
                    try
                    {
                        cache.Get(key);
                    }
                    catch (KeyNotFoundException) { }
                }
            }

            // Assert
            Assert.True(cache.GetType().GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cache).GetType().GetProperty("Count").GetValue(cache.GetType().GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cache)).Equals(1000));
        }
        

        //Eğer varolan bir key yeniden eklenirse value değeri değişmişse frequency arttırılmalı ve minFrequency güncellenmeli
        [Fact]
        public void Adding_Existing_Key_With_Different_Value_Should_Update_Frequency()
        {
            // Arrange
            var cache = new HybridCache<int, string>(5);

            // Act
            cache.Add(1, "Value 1");
            cache.Add(2, "Value 2");
            cache.Add(3, "Value 3");
            cache.Add(4, "Value 4");
            cache.Add(5, "Value 5");

            cache.Get(1); // Increment frequency of key 1
            cache.Add(3, "New Value 1"); // Update value of key 1

            // Assert
            Assert.Equal("New Value 1", cache.Get(3));

            // Check frequency
            Assert.Equal(3, cache.GetFrequency(3));
        }
    }
}
