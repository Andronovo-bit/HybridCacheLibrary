namespace HybridCacheLibrary.Tests.SizeBased
{
    public class HybridCacheTests
    {
        [Fact]
        public void Add_Get_Item_Should_Work()
        {
            var size = ObjectSizeCalculator.CalculateObjectSize("value1");
            // Arrange
            var cache = new SizeBasedHybridCache<string, string>(size); 

            // Act
            cache.Add("key1", "value1");
            var value = cache.Get("key1");

            // Assert
            Assert.Equal("value1", value);
        }

        [Fact]
        public void Get_Remaining_Capacity_Should_Work()
        {
            // Arrange
            var cache = new SizeBasedHybridCache<string, string>(1, CacheSizeType.Kilobytes);
            long size = 0;

            // Act
            for (int i = 0; i < 20; i++)
            {
                var key = $"value{i}";
                var value = $"value{i}";

                size += ObjectSizeCalculator.CalculateObjectSize(value);

                cache.Add(key, value);
            }

            // Assert

            Assert.Equal(cache.Capacity - size, cache.RemainingCacheSizeInBytes);
        
            Assert.Equal(size, cache.CurrentCacheSizeInBytes);

            Assert.Equal(1024, cache.Capacity);

        }

        [Fact]
        public void Get_NonExistent_Key_Should_Throw_Exception()
        {
            // Arrange
            var cache = new SizeBasedHybridCache<string, string>(20);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key2"));
        }

        [Fact]
        public void Evict_When_Capacity_Exceeded_Should_Remove_Lowest_Frequency_Item()
        {
            // Arrange
            var cache = new SizeBasedHybridCache<string, string>(44);

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
            // 16 byte reference + object header every reference type object

            // Arrange
            var cache = new SizeBasedHybridCache<string, string>(44); 

            // Act
            cache.Add("key1", "value1"); // 16 + 1 + 1 + 1 + 1 + 1 + 1 = 22
            cache.Add("key2", "value2"); // 16 + 1 + 1 + 1 + 1 + 1 + 1 = 22
            cache.SetCapacity(66);
            cache.Add("key3", "value3"); // 16 + 1 + 1 + 1 + 1 + 1 + 1 = 22

            Assert.Equal("value3", cache.Get("key3"));

            cache.SetCapacity(44); // This should evict the item with the lowest frequency

            // Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key1")); // Assuming key1 has the lowest frequency
            Assert.Equal("value2", cache.Get("key2"));
        }

        [Fact]
        public void Increasing_Frequency_Should_Protect_Item_From_Eviction()
        {
            // Arrange
            var cache = new SizeBasedHybridCache<string, string>(44);

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
            var cache = new SizeBasedHybridCache<string, string>(66);

            // Act
            cache.Add("key1", "value1");
            cache.Add("key2", "value2");
            cache.Add("key3", "value3");
            cache.SetCapacity(110); // This should evict all but one item
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
            var cache = new SizeBasedHybridCache<string, string>(66);

            // Act
            cache.Add("key1", "value1");
            cache.Add("key2", "value2");
            cache.Add("key3", "value3");
            cache.SetCapacity(22, true); // This should evict all but one item

            // Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key1"));
            Assert.Throws<KeyNotFoundException>(() => cache.Get("key2"));
            Assert.Equal("value3", cache.Get("key3")); // Only one item should remain
        }

        [Fact]
        public void Add_With_Frequency_Should_Set_Correct_Frequency()
        {
            // Arrange
            var cache = new SizeBasedHybridCache<int, string>(1, CacheSizeType.Kilobytes);

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
            var cache = new SizeBasedHybridCache<int, string>(22);

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
            var cache = new SizeBasedHybridCache<int, string>(66); // 22 bytes per item 3 items equals 66 bytes

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

        // create mock class and test
        [Fact]
        public void Add_Custom_Class_Should_Work()
        {
            /*
             * User => 8 object header + 8 byte reference = 16 bytes
             * Id => 1 => 4 byte int
             * Age => 30 => 4 byte int
             * John Doe => 8 byte reference + 8 byte string = 16 bytes
             *
             * Address => 8 object header + 8 byte reference = 16 bytes
             * City => Istanbul => 8 byte reference + 8 byte string = 16 bytes 
             * Country => Turkey => 8 byte reference + 6 byte string = 14 bytes
             * 
             * Numbers => 8 object header + (10 * 4 byte int) = 48 bytes
             * 
             * Chars => 8 object header + 5 byte char  = 13 bytes
             * 
             * Total => 16 + 4 + 4 + 16 + 16 + 16 + 14 + 48 + 13 = 147 bytes
             */

            var user = new User(1, "John Doe", 30, new Address("Istanbul", "Turkey"));

            user.Numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

            user.Chars = new List<char> { 'a', 'b', 'c', 'd', 'e' };

            var size = ObjectSizeCalculator.CalculateObjectSize(user);

            // Arrange
            var cache = new SizeBasedHybridCache<int, User>(256);

            // Act
            cache.Add(1, user);

            // Assert
            Assert.Equal(user, cache.Get(1));
            Assert.Equal(size, cache.CurrentCacheSizeInBytes);
        }

        [Fact]
        public async Task Concurrent_Add_And_Get_Should_Work_Correctly()
        {
            // Arrange
            var cache = new SizeBasedHybridCache<int, string>(100, CacheSizeType.Kilobytes); // 100 KB
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        int key = threadId * 100 + j;
                        cache.Add(key, $"Value {key}");
                        var value = cache.Get(key);  // Add işleminden hemen sonra Get işlemi
                        Assert.Equal($"Value {key}", value);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(100 * 10, cache.Count());
        }
        [Fact]
        public void SetCapacity_Should_Shrink_Cache_If_Needed()
        {
            // Arrange
            var cache = new SizeBasedHybridCache<int, string>(6, CacheSizeType.Kilobytes); // 6 KB
            var largeValue = new string('a', 3072); // 3 KB string 
            var smallValue = new string('b', 1024); // 1 KB string 

            cache.Add(1, largeValue); // 3 KB + 16 bytes (reference + object header)
            cache.Add(2, smallValue); // 1 KB + 16 bytes (reference + object header)
            cache.Add(3, smallValue); // 1 KB + 16 bytes (reference + object header)

            // Act
            cache.SetCapacity(3, shrink: true, CacheSizeType.Kilobytes); // Shrink to 3 KB

            // Assert
            Assert.Throws<KeyNotFoundException>(() => cache.Get(1)); // Large item should be evicted
            Assert.Equal(smallValue, cache.Get(2));
            Assert.Equal(smallValue, cache.Get(3));
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
        public int[] Numbers { get; set; }
        public List<char> Chars { get; set; }
    }
    internal record Address(string City, string Country);
}
