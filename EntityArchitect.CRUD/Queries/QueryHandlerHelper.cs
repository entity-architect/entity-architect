namespace EntityArchitect.CRUD.Queries;

public static class QueryHandlerHelper
{
    public static T BuildResponse<T>(params object[] args) where T : class
    {
        var mainType = args[0] as T;

        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            foreach (var argument in args.Skip(1).Take(args.Length - 1))
            {
                if(argument is null) continue;

                var argumentType = argument.GetType();
                if (property.PropertyType == argumentType)
                {
                    property.SetValue(mainType, argument);
                }
            }
        }
        
        return mainType;
    }
}