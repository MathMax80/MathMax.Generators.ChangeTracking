using Xunit;
using Shouldly;
using MathMax.ChangeTracking.Examples.Trackers;
using System;
using System.Linq;
using MathMax.ChangeTracking.Examples.CompoundKey;

namespace MathMax.ChangeTracking.Examples.Tests.Trackers;

public class SomeEntityChangeTrackerTests
{
    public SomeEntityChangeTrackerTestFixture Fixture { get; }

    public SomeEntityChangeTrackerTests()
    {
        Fixture = new SomeEntityChangeTrackerTestFixture();
    }

    [Fact]
    public void Collection_WithCompoundKey_ItemChanged_YieldsDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualSomeEntityPair();
        var leftChild = left.Children[0];
        var rightChild = right.Children[0];
        rightChild.Name = "X"; // change scalar

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();

        difference.ShouldNotBeNull();
        Console.WriteLine(difference);
        difference.Path.ShouldBe($"SomeEntity.Children[(KeyPartA={leftChild.KeyPartA},KeyPartB='{leftChild.KeyPartB}',KeyPartC={leftChild.KeyPartC})].Name");
        difference.Kind.ShouldBe(DifferenceKind.Modification);
        difference.LeftOwner.ShouldBe(leftChild);
        difference.LeftValue.ShouldBe(leftChild.Name);
        difference.RightOwner.ShouldBe(rightChild);
        difference.RightValue.ShouldBe(rightChild.Name);
    }

    [Fact]
    public void Collection_WithCompoundKey_ItemAdded_YieldsPresenceDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualSomeEntityPair();
        var newChildEntityKeyPartA = Guid.NewGuid();
        var newChildEntityKeyPartB = "C";
        var newChildEntityKeyPartC = 3;
        var newChildEntity = Fixture.CreateSomeChildEntity(newChildEntityKeyPartA, newChildEntityKeyPartB, newChildEntityKeyPartC);

        right.Children = [.. right.Children, newChildEntity]; // Add a child entity on right with new identity

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        difference.ShouldNotBeNull();
        Console.WriteLine(difference);
        difference.Path.ShouldBe($"SomeEntity.Children[(KeyPartA={newChildEntity.KeyPartA},KeyPartB='{newChildEntity.KeyPartB}',KeyPartC={newChildEntity.KeyPartC})]");
        difference.Kind.ShouldBe(DifferenceKind.Addition);
        difference.LeftOwner.ShouldBe(left.Children);
        difference.LeftValue.ShouldBeNull();
        difference.RightOwner.ShouldBe(right.Children);
        difference.RightValue.ShouldBe(newChildEntity);
    }

    [Fact]
    public void Collection_WithCompoundKey_ItemRemoved_YieldsPresenceDifference()
    {
        // Arrange
        var (left, right) = Fixture.CreateEqualSomeEntityPair();
        var leftChildEntity = left.Children[0];
        right.Children = [.. right.Children.Where(c => c.KeyPartA != leftChildEntity.KeyPartA || c.KeyPartB != leftChildEntity.KeyPartB || c.KeyPartC != leftChildEntity.KeyPartC)];

        // Act
        var differences = left.GetDifferences(right).ToArray();

        // Assert
        var difference = differences.ShouldHaveSingleItem();
        difference.ShouldNotBeNull();
        Console.WriteLine(difference);
        difference.Path.ShouldBe($"SomeEntity.Children[(KeyPartA={leftChildEntity.KeyPartA},KeyPartB='{leftChildEntity.KeyPartB}',KeyPartC={leftChildEntity.KeyPartC})]");
        difference.Kind.ShouldBe(DifferenceKind.Removal);
        difference.LeftOwner.ShouldBe(left.Children);
        difference.LeftValue.ShouldBe(leftChildEntity);
        difference.RightOwner.ShouldBe(right.Children);
        difference.RightValue.ShouldBeNull();
    }
}