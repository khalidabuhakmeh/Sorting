using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sorting.Tests
{
    public class OrderBy
    {
        private readonly ITestOutputHelper output;

        public class Thing
        {
            public int Id { get; set; } = 1;
            public string Name { get; set; } = "Test";
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        public OrderBy(ITestOutputHelper output)
        {
            this.output = output;
            Things = Enumerable
                .Range(1, 100)
                .Select(x => new Thing {Id = x})
                .AsQueryable();
        }

        public IQueryable<Thing> Things { get; set; }

        [Fact]
        public void By_QueryString()
        {
            var result = Things
                .OrderBy($"-{nameof(Thing.Id)}, {nameof(Thing.CreatedAt)}")
                .ToList();
            
            Assert.Equal(100, result[0].Id);
        }
        
        [Fact]
        public void By_Array()
        {
            var result = Things
                .OrderBy($"-{nameof(Thing.Id)}", $"{nameof(Thing.CreatedAt)}")
                .ToList();
            
            Assert.Equal(100, result[0].Id);
        }

        [Fact]
        public void Is_Ordered()
        {
            Assert.True(Things.OrderBy(x => x.Id).IsOrdered());
        }
        
        [Fact]
        public void Is_Not_Ordered()
        {
            Assert.False(Things.IsOrdered());
        }

        [Fact]
        public void ToString_Returns_String()
        {
            var value = $"{nameof(Thing.Id)},{nameof(Thing.CreatedAt)}";
            var sort = new SortCollection<Thing>(value);

            var actual = sort.ToString();
            output.WriteLine(actual);
            
            Assert.Equal(value, actual);
        }

        [Fact]
        public void With_NotExisting_Appends_Property()
        {
            var value = $"{nameof(Thing.Id)}";
            var sort = new SortCollection<Thing>(value);

            var actual = sort.AddOrUpdate("CreatedAt");
            output.WriteLine(actual);
            
            Assert.Equal("Id,CreatedAt", actual);
        }
        
        [Fact]
        public void With_Existing_Flips_Property()
        {
            var value = $"{nameof(Thing.Id)}";
            var sort = new SortCollection<Thing>(value);

            var actual = sort.AddOrUpdate("Id");
            output.WriteLine(actual);
            
            Assert.Equal("-Id", actual);
        }
        
        
        [Fact]
        public void Remove_Property()
        {
            var value = $"{nameof(Thing.Id)},{nameof(Thing.CreatedAt)}";
            var sort = new SortCollection<Thing>(value);

            var actual = sort.Remove("CreatedAt");
            output.WriteLine(actual);
            
            Assert.Equal("Id", actual);
        }

        [Fact]
        public void Can_create_null()
        {
            var sort = new SortCollection<Thing>();
            Assert.NotNull(sort);
        }
        
        
    }
}