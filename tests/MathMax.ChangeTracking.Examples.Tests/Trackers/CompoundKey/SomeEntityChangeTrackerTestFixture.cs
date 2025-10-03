using System;
using MathMax.ChangeTracking.Examples.CompoundKey;

namespace MathMax.ChangeTracking.Examples.Tests.Trackers;

public class SomeEntityChangeTrackerTestFixture
{
    public (SomeEntity, SomeEntity) CreateEqualSomeEntityPair()
    {
        var someEntityId = Guid.NewGuid();
        
        var childOneKeyPartA = Guid.NewGuid();
        var childOneKeyPartB = "A";
        var childOneKeyPartC = 1;

        var childTwoKeyPartA = Guid.NewGuid();
        var childTwoKeyPartB = "B";
        var childTwoKeyPartC = 2;

        var someEntity1 = CreateSomeEntity(someEntityId, childOneKeyPartA, childOneKeyPartB, childOneKeyPartC, childTwoKeyPartA, childTwoKeyPartB, childTwoKeyPartC);
        var someEntity2 = CreateSomeEntity(someEntityId, childOneKeyPartA, childOneKeyPartB, childOneKeyPartC, childTwoKeyPartA, childTwoKeyPartB, childTwoKeyPartC);

        return (someEntity1, someEntity2);
    }

    public SomeEntity CreateSomeEntity(Guid someEntityId, Guid childOneKeyPartA, string childOneKeyPartB, int childOneKeyPartC, Guid childTwoKeyPartA, string childTwoKeyPartB, int childTwoKeyPartC)
    {
        var someChildEntity1 = CreateSomeChildEntity(childOneKeyPartA, childOneKeyPartB, childOneKeyPartC);
        var someChildEntity2 = CreateSomeChildEntity(childTwoKeyPartA, childTwoKeyPartB, childTwoKeyPartC);
        return new SomeEntity
        {
            Id = someEntityId,
            Name = "Entity Name",
            Children = [someChildEntity1, someChildEntity2]
        };
    }

    public SomeChildEntity CreateSomeChildEntity(Guid childOneKeyPartA, string childOneKeyPartB, int childOneKeyPartC)
    {
        return new SomeChildEntity
        {
            KeyPartA = childOneKeyPartA,
            KeyPartB = childOneKeyPartB,
            KeyPartC = childOneKeyPartC,
            Name = "Child Name"
        };
    }
}
