using DuonDevKit.Core.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.Core.Tests.Mapping
{
    public class MapperTests
    {
        /// <summary>
        /// Builds an <see cref="IObjectMapper"/> from an explicit mapper type list rather than scanning
        /// <c>typeof(MapperTests).Assembly</c> wholesale — this test file deliberately keeps a couple of
        /// mapper fixtures (see <see cref="DuplicateOrderMapperA"/>/<see cref="DuplicateOrderMapperB"/>)
        /// that implement the same type pair on purpose, which a whole-assembly scan would (correctly)
        /// reject.
        /// </summary>
        private static IObjectMapper BuildMapper(params Type[] mapperTypes)
        {
            var services = new ServiceCollection();
            services.AddDuonDevKitMappers(mapperTypes);
            return services.BuildServiceProvider().GetRequiredService<IObjectMapper>();
        }

        [Fact]
        public void Map_RegisteredMapper_CreatesNewDestination()
        {
            var mapper = BuildMapper(typeof(OrderToOrderDtoMapper));
            var order = new Order { Name = "A", Total = 42m };

            var dto = mapper.Map<Order, OrderDto>(order);

            Assert.Equal("A", dto.Name);
            Assert.Equal(42m, dto.Total);
        }

        [Fact]
        public void Map_IntoExistingDestination_UpdatesInPlaceViaUpdateMapper()
        {
            var mapper = BuildMapper(typeof(UpdateOrderRequestToOrderMapper));
            var order = new Order { Name = "Old", Total = 10m };
            var request = new UpdateOrderRequest { Name = "New" };

            mapper.Map(request, order);

            Assert.Equal("New", order.Name);
            Assert.Equal(10m, order.Total); // untouched — the update mapper only maps Name
        }

        [Fact]
        public void MapList_MultipleItems_MapsEachOne()
        {
            var mapper = BuildMapper(typeof(OrderToOrderDtoMapper));
            var orders = new[] { new Order { Name = "A" }, new Order { Name = "B" } };

            var dtos = mapper.MapList<Order, OrderDto>(orders);

            Assert.Equal(2, dtos.Count);
            Assert.Equal(["A", "B"], dtos.Select(d => d.Name));
        }

        [Fact]
        public void MapToExtension_MirrorsIObjectMapperMap()
        {
            var mapper = BuildMapper(typeof(OrderToOrderDtoMapper));
            var order = new Order { Name = "A", Total = 5m };

            var dto = order.MapTo<Order, OrderDto>(mapper);

            Assert.Equal("A", dto.Name);
        }

        [Fact]
        public void MapToListExtension_MirrorsIObjectMapperMapList()
        {
            var mapper = BuildMapper(typeof(OrderToOrderDtoMapper));
            var orders = new[] { new Order { Name = "A" } };

            var dtos = orders.MapToList<Order, OrderDto>(mapper);

            Assert.Single(dtos);
        }

        [Fact]
        public void Map_MismatchedPropertyNamesAndComputedValues_MapsCorrectlyWithNoExtraConfiguration()
        {
            var mapper = BuildMapper(typeof(CustomerToCustomerSummaryDtoMapper));
            var customer = new Customer
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                EmailAddress = "ada@example.com",
                TotalSpent = 1500m,
            };

            var dto = mapper.Map<Customer, CustomerSummaryDto>(customer);

            Assert.Equal("Ada Lovelace", dto.FullName);  // two source fields combined into one
            Assert.Equal("ada@example.com", dto.Contact); // renamed field, no name matching involved
            Assert.True(dto.IsVip);                       // computed from a field with no destination counterpart
        }

        [Fact]
        public void Map_CalledRepeatedlyForSameTypePair_ResolvesTheMapperOnlyOnceEvenIfRegisteredTransient()
        {
            CountingOrderMapper.ConstructionCount = 0;
            var services = new ServiceCollection();
            services.AddTransient<IMapper<Order, CountedOrderDto>, CountingOrderMapper>(); // Transient: a plain
            // GetRequiredService call would construct a new instance every time — used here specifically so
            // "only constructed once" can only be explained by ObjectMapper's own cache, not by DI's built-in
            // scoped-instance reuse (which would mask the same outcome for a Scoped registration).
            services.AddScoped<IObjectMapper, ObjectMapper>();
            var mapper = services.BuildServiceProvider().GetRequiredService<IObjectMapper>();
            var order = new Order { Name = "A" };

            mapper.Map<Order, CountedOrderDto>(order);
            mapper.Map<Order, CountedOrderDto>(order);
            mapper.Map<Order, CountedOrderDto>(order);

            Assert.Equal(1, CountingOrderMapper.ConstructionCount);
        }

        [Fact]
        public void Map_NoMapperRegisteredForTypePair_Throws()
        {
            var mapper = BuildMapper(typeof(OrderToOrderDtoMapper));

            Assert.Throws<InvalidOperationException>(() => mapper.Map<UnrelatedType, OrderDto>(new UnrelatedType()));
        }

        [Fact]
        public void AddDuonDevKitMappers_NoAssembliesProvided_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentException>(() => services.AddDuonDevKitMappers());
        }

        [Fact]
        public void AddDuonDevKitMappers_TwoTypesImplementSameTypePair_ThrowsInsteadOfSilentlyPickingOne()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<InvalidOperationException>(() => services.AddDuonDevKitMappers(
                [typeof(DuplicateOrderMapperA), typeof(DuplicateOrderMapperB)]));

            Assert.Contains(nameof(DuplicateOrderMapperA), exception.Message);
            Assert.Contains(nameof(DuplicateOrderMapperB), exception.Message);
        }

        [Fact]
        public void AddDuonDevKitMappers_CalledTwiceWithSameTypePair_ThrowsOnSecondCallToo()
        {
            var services = new ServiceCollection();
            services.AddDuonDevKitMappers([typeof(DuplicateOrderMapperA)]);

            var exception = Assert.Throws<InvalidOperationException>(
                () => services.AddDuonDevKitMappers([typeof(DuplicateOrderMapperB)]));

            Assert.Contains(nameof(DuplicateOrderMapperA), exception.Message);
            Assert.Contains(nameof(DuplicateOrderMapperB), exception.Message);
        }

        [Fact]
        public void AddDuonDevKitMappers_ScansGivenAssembly_RegistersDiscoveredMappersExceptDeliberateDuplicates()
        {
            // Whole-assembly scanning is exercised here against a real assembly; DuplicateOrderMapperA/B
            // living in this same assembly is exactly the ambiguity this should reject.
            var services = new ServiceCollection();

            var exception = Assert.Throws<InvalidOperationException>(
                () => services.AddDuonDevKitMappers(typeof(MapperTests).Assembly));

            Assert.Contains(nameof(DuplicateOrderMapperA), exception.Message);
        }
    }
}
