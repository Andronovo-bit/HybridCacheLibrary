using System;
using Xunit;

namespace HybridCacheLibrary.Tests.CountBased
{
    public class HybridCacheTests
    {
        [Fact]
        public void Add_Get_Item_Should_Work()
        {
            // Arrange
            var cache = new CountBasedHybridCache<string, string>(3);

            // Act
            cache.Add("key1", "value1");
            var value = cache.Get("key1");

            // Assert
            Assert.Equal("value1", value);
        }

        [Fact]
        public void Get_NonExistent_Key_Should_Throw_Exception()
        {
            // Arrange
            var cache = new CountBasedHybridCache<string, string>(3);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key2"));
        }

        [Fact]
        public void Evict_When_Capacity_Exceeded_Should_Remove_Lowest_Frequency_Item()
        {
            // Arrange
            var cache = new CountBasedHybridCache<string, string>(2);

            // Act
            cache.Add("key1", "value1");
            cache.Add("key2", "value2");
            cache.Get("key1"); // Increment frequency of key1
            cache.Add("key3", "value3"); // This should evict key2 as it has the lowest frequency

            // Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key2"));
            Assert.Equal("value1", cache.Get("key1"));
            Assert.Equal("value3", cache.Get("key3"));
        }

        [Fact]
        public void SetCapacity_Should_Trim_Cache_If_Needed()
        {
            // Arrange
            var cache = new CountBasedHybridCache<string, string>(2);

            // Act
            cache.Add("key1", "value1");
            cache.Add("key2", "value2");
            cache.SetCapacity(3);
            cache.Add("key3", "value3");

            Assert.Equal("value3", cache.Get("key3"));

            cache.SetCapacity(2); // This should evict the item with the lowest frequency

            // Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key1")); // Assuming key1 has the lowest frequency
            Assert.Equal("value2", cache.Get("key2"));
        }

        [Fact]
        public void Increasing_Frequency_Should_Protect_Item_From_Eviction()
        {
            // Arrange
            var cache = new CountBasedHybridCache<string, string>(2);

            // Act
            cache.Add("key1", "value1");
            cache.Add("key2", "value2");
            cache.Get("key1"); // Increment frequency of key1
            cache.Add("key3", "value3"); // This should evict key2 instead of key1

            // Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key2"));
            Assert.Equal("value1", cache.Get("key1"));
            Assert.Equal("value3", cache.Get("key3"));
        }

        [Fact]
        public void Increasing_Capacity_All_Items_Should_Work_Properly()
        {
            // Arrange
            var cache = new CountBasedHybridCache<string, string>(3);

            // Act
            cache.Add("key1", "value1");
            cache.Add("key2", "value2");
            cache.Add("key3", "value3");
            cache.SetCapacity(5); // This should evict all but one item
            cache.Add("key4", "value4");
            cache.Add("key5", "value5");

            // Assert
            Assert.Equal("value3", cache.Get("key3"));
            Assert.Equal("value4", cache.Get("key4"));
            Assert.Equal("value5", cache.Get("key5"));

        }

        [Fact]
        public void Evicting_All_Items_Should_Work_Properly()
        {
            // Arrange
            var cache = new CountBasedHybridCache<string, string>(3);

            // Act
            cache.Add("key1", "value1");
            cache.Add("key2", "value2");
            cache.Add("key3", "value3");
            cache.SetCapacity(1, true); // This should evict all but one item

            // Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key1"));
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key2"));
            Assert.Equal("value3", cache.Get("key3")); // Only one item should remain
        }

        [Fact]
        public void Add_With_Frequency_Should_Set_Correct_Frequency()
        {
            // Arrange
            var cache = new CountBasedHybridCache<int, string>(10);

            // Act
            cache.Add(1, "Value1", 5); // Frequency 5
            cache.Add(2, "Value2", 10); // Frequency 10
            cache.Add(3, "Value3", 1); // Frequency 1

            // Assert
            Assert.Equal("Value1", cache.Get(1));
            Assert.Equal(6, cache.GetFrequency(1)); // Frequency should be 6

            Assert.Equal("Value2", cache.Get(2));
            Assert.Equal(11, cache.GetFrequency(2)); // Frequency should be 11

            Assert.Equal("Value3", cache.Get(3));
            Assert.Equal(2, cache.GetFrequency(3)); // Frequency should be 2
        }

        [Fact]
        public void Add_Without_Frequency_Should_Default_To_2()
        {
            // Arrange
            var cache = new CountBasedHybridCache<int, string>(10);

            // Act
            cache.Add(1, "Value1"); // Default frequency 1

            // Assert
            Assert.Equal("Value1", cache.Get(1)); // ++ frequency
            Assert.Equal(2, cache.GetFrequency(1)); // Frequency should be 2
        }

        [Fact]
        public void Add_With_Frequency_Should_Not_Evict_High_Frequency_Items()
        {
            // Arrange
            var cache = new CountBasedHybridCache<int, string>(3);

            // Act
            cache.Add(1, "Value1", 10); // High frequency
            cache.Add(2, "Value2", 1);  // Low frequency
            cache.Add(3, "Value3", 3);  // Low frequency

            // Adding one more item should evict one of the low frequency items
            cache.Add(4, "Value4", 1);

            // Assert
            Assert.Equal("Value1", cache.Get(1)); // High frequency item should remain
            Assert.Equal(11, cache.GetFrequency(1)); // Frequency should be 11

            // One of these should be evicted
            Assert.Throws<KeyNotFoundException>(() => cache.Get(2));

            Assert.Equal("Value4", cache.Get(4));
            Assert.Equal(2, cache.GetFrequency(4)); // Frequency should be 2
        }
    }
}
