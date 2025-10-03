// using System.Collections.Generic;

// namespace MathMax.ChangeTracking.Examples.Trackers;

// public static class ExampleOfGeneratedPersonChangeTracker
// {
//     public static IEnumerable<Difference> GetDifferences(this Person right, Person left, string path = nameof(Person))
//     {
//         if (left == null && right == null)
//         {
//             yield break; // no difference
//         }

//         if (left == null || right == null)
//         {
//             yield return new Difference
//             {
//                 Path = path,
//                 LeftOwner = left,
//                 RightOwner = right,
//                 LeftValue = left,
//                 RightValue = right
//             };
//             yield break; // difference reported; no deeper recursion possible
//         }

//         foreach (var diff in DiffScalarProperties(left, right, path))
//         {
//             yield return diff;
//         }

//         if (left.Addresses != null || right.Addresses != null)
//         {
//             var leftList = left.Addresses ?? [];
//             var rightList = right.Addresses ?? [];

//             foreach (var diff in ChangeTrackerExtensions.DiffListByIdentity(leftList, rightList, path + ".Addresses", (leftAddress, rightAddress, pth) => leftAddress.GetDifferences(rightAddress, pth), a => (a.ZipCode, a.HouseNumber)))
//             {
//                 yield return diff;
//             }
//         }
//         if (left.Orders != null || right.Orders != null)
//         {
//             var leftList = left.Orders ?? [];
//             var rightList = right.Orders ?? [];

//             foreach (var diff in ChangeTrackerExtensions.DiffListByIdentity(leftList, rightList, path + ".Orders", (leftOrder, rightOrder, pth) => leftOrder.GetDifferences(rightOrder, pth), o => o.OrderId))
//             {
//                 yield return diff;
//             }
//         }
//     }

//     private static IEnumerable<Difference> GetDifferences(this Address left, Address right, string path = nameof(Address))
//     {
//         if (left == null && right == null)
//         {
//             yield break; // no difference
//         }

//         if (left == null || right == null)
//         {
//             yield return new Difference
//             {
//                 Path = path,
//                 LeftOwner = left,
//                 RightOwner = right,
//                 LeftValue = left,
//                 RightValue = right
//             };
//             yield break; // difference reported; no deeper recursion possible
//         }

//         foreach (var diff in DiffScalarProperties(left, right, path))
//         {
//             yield return diff;
//         }
//     }

//     private static IEnumerable<Difference> GetDifferences(this Order left, Order right, string path = nameof(Order))
//     {
//         if (left == null && right == null)
//         {
//             yield break; // no difference
//         }

//         if (left == null || right == null)
//         {
//             yield return new Difference
//             {
//                 Path = path,
//                 LeftOwner = left,
//                 RightOwner = right,
//                 LeftValue = left,
//                 RightValue = right
//             };
//             yield break; // difference reported; no deeper recursion possible
//         }

//         foreach (var diff in DiffScalarProperties(left, right, path))
//         {
//             yield return diff;
//         }

//         if (left.Items != null || right.Items != null)
//         {
//             var leftList = left.Items ?? [];
//             var rightList = right.Items ?? [];

//             foreach (var diff in ChangeTrackerExtensions.DiffListByIdentity(leftList, rightList, path + ".Items", (leftItem, rightItem, pth) => leftItem.GetDifferences(rightItem, pth), i => i.ProductId))
//             {
//                 yield return diff;
//             }
//         }
//     }

//     private static IEnumerable<Difference> GetDifferences(this OrderItem left, OrderItem right, string path = nameof(OrderItem))
//     {
//         if (left == null && right == null)
//         {
//             yield break; // no difference
//         }

//         if (left == null || right == null)
//         {
//             yield return new Difference
//             {
//                 Path = path,
//                 LeftOwner = left,
//                 RightOwner = right,
//                 LeftValue = left,
//                 RightValue = right
//             };
//             yield break; // difference reported; no deeper recursion possible
//         }

//         foreach (var diff in DiffScalarProperties(left, right, path))
//         {
//             yield return diff;
//         }
//     }

//     private static IEnumerable<Difference> DiffScalarProperties(OrderItem left, OrderItem right, string path)
//     {
//         if (left.ProductId != right.ProductId)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(OrderItem.ProductId), left, right, left.ProductId, right.ProductId);
//         }
//         if (left.Quantity != right.Quantity)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(OrderItem.Quantity), left, right, left.Quantity, right.Quantity);
//         }
//         if (left.UnitPrice != right.UnitPrice)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(OrderItem.UnitPrice), left, right, left.UnitPrice, right.UnitPrice);
//         }
//     }

//     private static IEnumerable<Difference> DiffScalarProperties(Order left, Order right, string path)
//     {
//         if (left.OrderId != right.OrderId)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Order.OrderId), left, right, left.OrderId, right.OrderId);
//         }
//         if (left.OrderDate != right.OrderDate)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Order.OrderDate), left, right, left.OrderDate, right.OrderDate);
//         }
//     }

//     private static IEnumerable<Difference> DiffScalarProperties(Address left, Address right, string path)
//     {
//         if (left.Street != right.Street)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Address.Street), left, right, left.Street, right.Street);
//         }
//         if (left.City != right.City)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Address.City), left, right, left.City, right.City);
//         }
//         if (left.ZipCode != right.ZipCode)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Address.ZipCode), left, right, left.ZipCode, right.ZipCode);
//         }
//         if (left.HouseNumber != right.HouseNumber)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Address.HouseNumber), left, right, left.HouseNumber, right.HouseNumber);
//         }
//     }

//     private static IEnumerable<Difference> DiffScalarProperties(Person left, Person right, string path)
//     {
//         if (left.PersonId != right.PersonId)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Person.PersonId), left, right, left.PersonId, right.PersonId);
//         }
//         if (left.FirstName != right.FirstName)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Person.FirstName), left, right, left.FirstName, right.FirstName);
//         }
//         if (left.LastName != right.LastName)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Person.LastName), left, right, left.LastName, right.LastName);
//         }
//         if (left.DateOfBirth != right.DateOfBirth)
//         {
//             yield return ChangeTrackerExtensions.CreatePropertyDifference(path, nameof(Person.DateOfBirth), left, right, left.DateOfBirth, right.DateOfBirth);
//         }
//     }
// }