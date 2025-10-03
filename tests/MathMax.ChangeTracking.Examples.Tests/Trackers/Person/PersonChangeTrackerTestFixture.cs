using System;
using System.Collections.Generic;

namespace MathMax.ChangeTracking.Examples.Tests.Trackers;

public class PersonChangeTrackerTestFixture
{
    public (Person, Person) CreateEqualPersonPair()
    {
        var id = Guid.NewGuid();
        var person1 = CreatePerson(id);
        var person2 = CreatePerson(id);
        return (person1, person2);
    }

    public Person CreatePerson(Guid id)
    {
        return new Person
        {
            PersonId = id,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Address = CreateAddress(),
            Orders = CreateOrders()
        };
    }

    public IReadOnlyList<Order> CreateOrders()
    {
        return
        [
            CreateOrder(1),
            CreateOrder(2)
        ];
    }

    public Order CreateOrder(int orderId)
    {
        return new Order
        {
            OrderId = orderId,
            OrderDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Items = CreateItems(orderId)
        };
    }

    public IReadOnlyList<OrderItem> CreateItems(int orderId)
    {
        return
        [
            CreateItem(orderId,1),
            CreateItem(orderId,2)
        ];
    }

    public OrderItem CreateItem(int orderId, int productId)
    {
        return new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = 1,
            UnitPrice = 9.99m,
        };
    }

    public Address CreateAddress()
    {
        return new Address
        {
            Street = "Main St",
            City = "Any Town",
            State = "CA",
            ZipCode = "12345",
            HouseNumber = "67"
        };
    }
}
