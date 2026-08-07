using DuonDevKit.Core.Mapping;

namespace DuonDevKit.Core.Tests.Mapping
{
    public class Order
    {
        public string Name { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class OrderDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class UpdateOrderRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class OrderToOrderDtoMapper : IMapper<Order, OrderDto>
    {
        public OrderDto Map(Order source) => new() { Name = source.Name, Total = source.Total };
    }

    /// <summary>Distinct destination type used only by <see cref="CountingOrderMapper"/>, so that fixture never collides with <see cref="OrderToOrderDtoMapper"/>'s registration when a test scans this whole assembly.</summary>
    public class CountedOrderDto
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Counts its own constructions — registered as Transient in a test to prove ObjectMapper's per-type-pair
    /// cache (not DI's built-in scoped caching) is what keeps a single instance across repeated Map() calls.
    /// </summary>
    public class CountingOrderMapper : IMapper<Order, CountedOrderDto>
    {
        public static int ConstructionCount;

        public CountingOrderMapper() => ConstructionCount++;

        public CountedOrderDto Map(Order source) => new() { Name = source.Name };
    }

    public class UpdateOrderRequestToOrderMapper : IUpdateMapper<UpdateOrderRequest, Order>
    {
        public void Map(UpdateOrderRequest source, Order destination) => destination.Name = source.Name;
    }

    /// <summary>Not a mapper — should never be picked up by assembly scanning.</summary>
    public class UnrelatedType
    {
    }

    // Deliberately mismatched shape: renamed field (EmailAddress -> Contact), two fields combined into
    // one (FirstName + LastName -> FullName), and a value computed from a source field with no
    // destination counterpart at all (TotalSpent -> IsVip). None of this needs any special
    // configuration — it's just what the hand-written Map() method body does.
    public class Customer
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
    }

    public class CustomerSummaryDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public bool IsVip { get; set; }
    }

    public class CustomerToCustomerSummaryDtoMapper : IMapper<Customer, CustomerSummaryDto>
    {
        private const decimal VipThreshold = 1000m;

        public CustomerSummaryDto Map(Customer source) => new()
        {
            FullName = $"{source.FirstName} {source.LastName}".Trim(),
            Contact = source.EmailAddress,
            IsVip = source.TotalSpent >= VipThreshold,
        };
    }

    /// <summary>Dedicated, otherwise-unused type pair for the duplicate-registration test — deliberately implemented by two mappers below.</summary>
    public class DuplicateTestSource
    {
    }

    public class DuplicateTestDestination
    {
    }

    public class DuplicateOrderMapperA : IMapper<DuplicateTestSource, DuplicateTestDestination>
    {
        public DuplicateTestDestination Map(DuplicateTestSource source) => new();
    }

    public class DuplicateOrderMapperB : IMapper<DuplicateTestSource, DuplicateTestDestination>
    {
        public DuplicateTestDestination Map(DuplicateTestSource source) => new();
    }
}
