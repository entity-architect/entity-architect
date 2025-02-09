using System.Linq;
using EntityArchitect.CRUD.Attributes.QueryResponseTypeAttributes;
using EntityArchitect.CRUD.Enumerations;

namespace EntityArchitect.CRUD.Queries;

public static class QueryHandlerHelper
{
    public static T BuildResponse<T>(params object[] args) where T : class
    {
        var mainType = args[0] as T;

        var properties = typeof(T).GetProperties();
        var i = 1;


        foreach (var argument in args.Skip(1).Take(args.Length - 1))
        foreach (var property in properties)
        {
            if (argument is null) continue;

            var argumentType = argument.GetType();
            if (property.PropertyType == argumentType)
            {
                if(property.CustomAttributes.Any(x => x.AttributeType == typeof(IsEnumerationAttribute)))
                {
                    var value = typeof(Enumeration).GetMethod("GetById")?.MakeGenericMethod(property.PropertyType)
                        .Invoke(null, new[] { argument });
                    property.SetValue(mainType, value);
                }
                if (property.CustomAttributes.Any(x => x.AttributeType == typeof(NestedTypeAttribute)))
                {
                    var method =
                        typeof(QueryHandlerHelper).GetMethod(nameof(BuildResponse))!.MakeGenericMethod(
                            property.PropertyType);
                    var nestedType = method.Invoke(null, new object[] { args.Skip(i).ToArray() });

                    property.SetValue(mainType, nestedType ?? argument);
                    i++;
                }
                else
                {
                    property.SetValue(mainType, argument);
                }
            }
        }

        return mainType;
    }
}