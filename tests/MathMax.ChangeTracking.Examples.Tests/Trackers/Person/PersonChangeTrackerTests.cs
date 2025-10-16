using System;
using System.Linq;
using MathMax.ChangeTracking.Examples.Trackers;
using Shouldly;
using Xunit;

namespace MathMax.ChangeTracking.Examples.Tests.Trackers;

public class PersonChangeTrackerTests
{
    public PersonChangeTrackerTestFixture Fixture { get; }

    public PersonChangeTrackerTests()
    {
        Fixture = new PersonChangeTrackerTestFixture();
    }

    [Fact]
    public void NoChanges_YieldsNoDifferences()
    {
        // Arrange
        var (person1, person2) = Fixture.CreateEqualPersonPair();

        // Act
        var differences = person1.GetDifferences(person2);

        // Assert
        differences.ShouldBeEmpty();
    }

    [Fact]
    public void ScalarPropertyChanged_YieldsModificationDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualPersonPair();
        left.FirstName = "X"; // change scalar

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);

        difference.ShouldNotBeNull();
        difference.Path.ShouldBe("Person.FirstName");
        difference.Kind.ShouldBe(DifferenceKind.Modification);
        difference.LeftOwner.ShouldBe(left);
        difference.LeftValue.ShouldBe(left.FirstName);
        difference.RightOwner.ShouldBe(right);
        difference.RightValue.ShouldBe(right.FirstName);
    }

    [Fact]
    public void ComplexProperty_NullToValue_YieldsModificationDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualPersonPair();
        left.Address = null; // remove

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);

        difference.ShouldNotBeNull();
        difference.Path.ShouldBe("Person.Address");
        difference.Kind.ShouldBe(DifferenceKind.Modification);
        difference.LeftOwner.ShouldBe(left);
        difference.LeftValue.ShouldBeNull();
        difference.RightOwner.ShouldBe(right);
        difference.RightValue.ShouldBe(right.Address);
    }

    [Fact]
    public void ComplexProperty_ValueToNull_YieldsPresenceDiff()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualPersonPair();
        right.Address = null; // remove

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);

        difference.ShouldNotBeNull();
        difference.Path.ShouldBe("Person.Address");
        difference.Kind.ShouldBe(DifferenceKind.Modification);
        difference.LeftOwner.ShouldBe(left);
        difference.LeftValue.ShouldBe(left.Address);
        difference.RightOwner.ShouldBe(right);
        difference.RightValue.ShouldBeNull();
    }

    [Fact]
    public void ComplexProperty_ScalarPropertyChanged_YieldsNestedDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualPersonPair();
        right.Address!.City = "NewCity"; // change scalar property of complex property

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);

        difference.ShouldNotBeNull();
        difference.Path.ShouldBe("Person.Address.City");
        difference.Kind.ShouldBe(DifferenceKind.Modification);
        difference.LeftOwner.ShouldBe(left.Address);
        difference.LeftValue.ShouldBe(left.Address!.City);
        difference.RightOwner.ShouldBe(right.Address);
        difference.RightValue.ShouldBe(right.Address!.City);
    }

    [Fact]
    public void Collection_ItemAdded_YieldsPresenceDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualPersonPair();
        var newOrder = Fixture.CreateOrder(999);
        right.Orders = [.. right.Orders, newOrder]; // Add an order on right with new identity

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);
        difference.ShouldNotBeNull();
        difference.Path.ShouldBe($"Person.Orders[(OrderId={newOrder.OrderId})]");
        difference.Kind.ShouldBe(DifferenceKind.Addition);
        difference.LeftOwner.ShouldBe(left.Orders);
        difference.LeftValue.ShouldBeNull();
        difference.RightOwner.ShouldBe(right.Orders);
        difference.RightValue.ShouldBe(newOrder);
    }

    [Fact]
    public void Collection_ItemRemoved_YieldsPresenceDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualPersonPair();
        var leftOrder = left.Orders[0];
        right.Orders = [.. right.Orders.Where(o => o.OrderId != leftOrder.OrderId)];

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);
        difference.ShouldNotBeNull();
        difference.Path.ShouldBe($"Person.Orders[(OrderId={leftOrder.OrderId})]");
        difference.Kind.ShouldBe(DifferenceKind.Removal);
        difference.LeftOwner.ShouldBe(left.Orders);
        difference.LeftValue.ShouldBe(leftOrder);
        difference.RightOwner.ShouldBe(right.Orders);
        difference.RightValue.ShouldBeNull();
    }

    [Fact]
    public void Collection_OrderDifference_YieldsNoDifferences()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualPersonPair();
        right.Orders = [right.Orders[1], right.Orders[0]]; // Swap order of existing items

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        differences.ShouldBeEmpty();
    }

    [Fact]
    public void Collection_ItemScalarPropertyChanged_YieldsNestedDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualPersonPair();
        var leftOrder = left.Orders[0];
        var rightOrder = right.Orders[0];
        rightOrder.OrderDate = leftOrder.OrderDate.AddDays(1);

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        difference.ShouldNotBeNull();
        difference.Path.ShouldBe($"Person.Orders[(OrderId={leftOrder.OrderId})].OrderDate");
        difference.Kind.ShouldBe(DifferenceKind.Modification);
        difference.LeftOwner.ShouldBe(leftOrder);
        difference.LeftValue.ShouldBe(leftOrder.OrderDate);
        difference.RightOwner.ShouldBe(rightOrder);
        difference.RightValue.ShouldBe(rightOrder.OrderDate);
    }

    [Fact]
    public void NestedCollection_ItemAdded_YieldsPresenceDifference()
    {
        var (left, right) = Fixture.CreateEqualPersonPair();

        var leftOrder = left.Orders[0];
        var rightOrder = right.Orders[0];
        var newItem = Fixture.CreateItem(rightOrder.OrderId, 77);

        rightOrder.Items = [.. rightOrder.Items, newItem];

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);
        difference.ShouldNotBeNull();
        difference.Path.ShouldBe($"Person.Orders[(OrderId={rightOrder.OrderId})].Items[(OrderId={newItem.OrderId},ProductId={newItem.ProductId})]");
        difference.Kind.ShouldBe(DifferenceKind.Addition);
        difference.LeftOwner.ShouldBe(leftOrder.Items);
        difference.LeftValue.ShouldBeNull();
        difference.RightOwner.ShouldBe(rightOrder.Items);
        difference.RightValue.ShouldBe(newItem);
    }

    [Fact]
    public void NestedCollection_ItemRemoved_YieldsPresenceDifference()
    {
        var (left, right) = Fixture.CreateEqualPersonPair();

        var leftOrder = left.Orders[0];
        var rightOrder = right.Orders[0];

        var leftItem = leftOrder.Items[0];
        var rightItem = rightOrder.Items[0];
        rightOrder.Items = [.. rightOrder.Items.Where(i => i.OrderId != rightItem.OrderId || i.ProductId != rightItem.ProductId)];

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);
        difference.ShouldNotBeNull();
        difference.Path.ShouldBe($"Person.Orders[(OrderId={rightItem.OrderId})].Items[(OrderId={rightItem.OrderId},ProductId={rightItem.ProductId})]");
        difference.Kind.ShouldBe(DifferenceKind.Removal);
        difference.LeftOwner.ShouldBe(left.Orders[0].Items);
        difference.LeftValue.ShouldBe(leftItem);
        difference.RightOwner.ShouldBe(right.Orders[0].Items);
        difference.RightValue.ShouldBeNull();
    }

    [Fact]
    public void RootObjectAdded_YieldsPresenceDifference()
    {
        // Arrange
        Person? left = null;
        Person right = Fixture.CreatePerson(Guid.NewGuid());

        // Act
        var differences = left!.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);
        difference.ShouldNotBeNull();
        difference.Path.ShouldBe("Person");
        difference.Kind.ShouldBe(DifferenceKind.Addition);
        difference.LeftOwner.ShouldBeNull();
        difference.LeftValue.ShouldBeNull();
        difference.RightOwner.ShouldBeNull();
        difference.RightValue.ShouldBe(right);
    }

    [Fact]
    public void RootObjectRemoved_YieldsPresenceDifference()
    {
        // Arrange
        Person left = Fixture.CreatePerson(Guid.NewGuid());
        Person? right = null;

        // Act
        var differences = left.GetDifferences(right!).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        Console.WriteLine(difference);
        difference.ShouldNotBeNull();
        difference.Path.ShouldBe("Person");
        difference.Kind.ShouldBe(DifferenceKind.Removal);
        difference.LeftOwner.ShouldBeNull();
        difference.LeftValue.ShouldBe(left);
        difference.RightOwner.ShouldBeNull();
        difference.RightValue.ShouldBeNull();
    }

}
