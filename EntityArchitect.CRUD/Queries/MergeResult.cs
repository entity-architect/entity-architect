using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EntityArchitect.CRUD.Enumerations;

namespace EntityArchitect.CRUD.Queries;

internal static class MergeResult
{
private class IdBasedComparer : IEqualityComparer<object>
    {
        public bool Equals(object x, object y)
        {
            if (x == null || y == null) return x == y;
            var typeX = x.GetType();
            var typeY = y.GetType();

            // Jeśli różne typy, to nie są sobie równe.
            if (typeX != typeY) return false;

            // Szukamy właściwości "Id" lub "id".
            var propX = typeX.GetProperty("id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propX == null) // jeśli brak pola Id, porównaj standardowo.
                return x.Equals(y);

            var idX = propX.GetValue(x);
            var idY = propX.GetValue(y);
            // Porównujemy wartości ID.
            return Equals(idX, idY);
        }

        public int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            var type = obj.GetType();
            var prop = type.GetProperty("id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return obj.GetHashCode();

            var value = prop.GetValue(obj);
            return value?.GetHashCode() ?? 0;
        }
    }

   private static object MergeObjects(object obj1, object obj2)
{
    if (obj1 == null) return obj2;
    if (obj2 == null) return obj1;
    
    var type = obj1.GetType();
    if (type != obj2.GetType())
        throw new InvalidOperationException("Objects must be of the same type to merge.");

    // Jeśli to klasa dziedzicząca po Enumeration, wystarczy zwrócić pierwszy nie-null.
    // (Zgodnie z Twoim kodem — tu można też dowolnie inaczej obsługiwać takie typy.)
    if (type.BaseType == typeof(Enumeration))
        return obj1 ?? obj2;

    // Tworzymy nową instancję obiektu docelowego.
    var merged = Activator.CreateInstance(type)!;

    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        if (!property.CanRead || !property.CanWrite)
            continue;

        var value1 = property.GetValue(obj1);
        var value2 = property.GetValue(obj2);

        // Jeśli to lista (implementująca IList), to łączymy elementy „po Id” i rekurencyjnie scalamy.
        if (value1 is IList list1 && value2 is IList list2)
        {
            // Tworzymy pustą listę tego samego typu co w docelowej klasie.
            var mergedList = (IList)Activator.CreateInstance(property.PropertyType)!;

            // Przygotowujemy się do wyszukiwania duplikatów w list2.
            var visited = new HashSet<object>();
            var comparer = new IdBasedComparer(); // Twój porównywacz oparty o właściwość Id.

            // Najpierw iterujemy po elementach list1.
            foreach (var item1 in list1)
            {
                // Czy w list2 istnieje element z tym samym Id?
                var item2 = list2.Cast<object>().FirstOrDefault(x => comparer.Equals(item1, x));
                if (item2 != null)
                {
                    // Jeśli znaleźliśmy parę o tym samym Id, rekurencyjnie je scalamy:
                    visited.Add(item2);
                    mergedList.Add(MergeObjects(item1, item2));
                }
                else
                {
                    // Brak elementu o wspólnym Id, więc po prostu dodajemy item1.
                    mergedList.Add(item1);
                }
            }

            // Teraz dodajemy z list2 te elementy, których nie dopasowaliśmy do żadnego z list1.
            foreach (var item2 in list2)
            {
                if (!visited.Contains(item2))
                {
                    mergedList.Add(item2);
                }
            }

            property.SetValue(merged, mergedList);
        }
        else if (property.PropertyType.IsClass && 
                 property.PropertyType != typeof(string) &&
                 value1 != null && value2 != null)
        {
            property.SetValue(merged, MergeObjects(value1, value2));
        }
        else
        {
            property.SetValue(merged, value1 ?? value2);
        }
    }

    return merged;
}

internal static object ConvertType(Type resultType, object obj)
    {
        if (resultType.BaseType == typeof(Enumeration))
        {
            var value = typeof(Enumeration).GetMethod("GetById")?.MakeGenericMethod(resultType)
                .Invoke(null, new[] { obj });
            return value!;
        }
        var instance = Activator.CreateInstance(resultType);

        var properties = resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var obj2 = obj.GetType().GetProperty(property.Name)?.GetValue(obj);
                if (obj2 is not null)
                {
                    var value = ConvertType(property.PropertyType.GetGenericArguments()[0], obj2);

                    var method = property.PropertyType.GetMethod(nameof(List<object>.Add));
                    var instanceValue = Activator.CreateInstance(property.PropertyType);

                    method!.Invoke(instanceValue, new[] { value });
                    property.SetValue(instance, instanceValue);
                    continue;
                }

                var emptyList = Activator.CreateInstance(property.PropertyType);
                property.SetValue(instance, emptyList);
            }
            else if (property.PropertyType is { IsClass: true, IsGenericType: false } &&
                     property.PropertyType != typeof(string) && property.PropertyType != typeof(DateTime) &&
                     property.PropertyType != typeof(Guid))
            {
                var obj2 = obj.GetType().GetProperty(property.Name)?.GetValue(obj);
                if (obj2 is not null)
                {
                    var value = ConvertType(property.PropertyType, obj2);
                    property.SetValue(instance, value);
                    continue;
                }

                property.SetValue(instance, null);
            }
            else
            {
                var value = obj.GetType().GetProperty(property.Name)?.GetValue(obj);
                property.SetValue(instance, value);
            }
        }

        return instance!;
    }

    internal static object MergeAllObjects(IEnumerable<object> objects)
    {
        var objectList = objects.ToList();
        var result = objectList.First();

        foreach (var obj in objectList.Skip(1))
        {
            result = MergeObjects(result, obj);
        }

        return result;
    }
}
