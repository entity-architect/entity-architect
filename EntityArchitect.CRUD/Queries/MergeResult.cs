using System.Collections;
using System.Reflection;

namespace EntityArchitect.CRUD.Queries;

internal static class MergeResult
{
    private static object MergeObjects(object obj1, object obj2)
    {
        var type = obj1.GetType();
        if (type != obj2.GetType()) throw new InvalidOperationException("Objects must be of the same type to merge.");

        var merged = Activator.CreateInstance(type)!;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite) continue;

            var value1 = property.GetValue(obj1);
            var value2 = property.GetValue(obj2);

            if (value1 is IList list1 && value2 is IList list2)
            {
                // Merge lists
                var mergedList = (IList)Activator.CreateInstance(property.PropertyType)!;
                foreach (var item in list1) mergedList.Add(item);
                foreach (var item in list2) mergedList.Add(item);
                property.SetValue(merged, mergedList);
            }
            else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                property.SetValue(merged, MergeObjects(value1!, value2!));
            }
            else
            {
                property.SetValue(merged, value1 ?? value2);
            }
        }

        return merged;
    }

    internal static object MergeAllObjects(IEnumerable<object> objects)
    {
        var objectList = objects.ToList();
        var result = objectList.First();

        foreach (var obj in objectList.Skip(1)) result = MergeObjects(result, obj);

        return result;
    }


    internal static object ConvertType(Type resultType, object obj)
    {
        var instance = Activator.CreateInstance(resultType);

        var properties = resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
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

        return instance!;
    }
}