using System;
using Xunit;
using HybridCacheLibrary;

namespace HybridCacheLibrary.Tests
{
    public class HybridCacheTests
    {
        [Fact]
        public void Add_Get_Item_Should_Work()
        {
            // Arrange
            var cache = new HybridCache<string, string>(3);

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
            var cache = new HybridCache<string, string>(3);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key2"));
        }

        [Fact]
        public void Evict_When_Capacity_Exceeded_Should_Remove_Lowest_Frequency_Item()
        {
            // Arrange
            var cache = new HybridCache<string, string>(2);

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
            var cache = new HybridCache<string, string>(2);

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
            var cache = new HybridCache<string, string>(2);

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
            var cache = new HybridCache<string, string>(3);

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
            var cache = new HybridCache<string, string>(3);

            // Act
            cache.Add("key1", "value1");
            cache.Add("key2", "value2");
            cache.Add("key3", "value3");
            cache.SetCapacity(1,true); // This should evict all but one item

            // Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key1"));
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key2"));
            Assert.Equal("value3", cache.Get("key3")); // Only one item should remain
        }
    }
}
