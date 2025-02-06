using System.Reflection;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.CustomEndpoints;

public static class CustomEndpointBuilder
{
    public static List<CustomEndpoint<T>> Build<T>(Assembly assembly, IServiceScope serviceProvider) where T : Entity
    {
        var types = assembly.GetTypes().Where(c => c.BaseType == typeof(CustomEndpoint<T>)).ToList();

        List<CustomEndpoint<T>> endpoints = new();
        foreach (var item in types)
        {
            var parameters = item.GetConstructors().First().GetParameters();

            List<object> parameterObjects = [];
            
            foreach (var parameter in parameters)
            {
                var service = serviceProvider.ServiceProvider.GetRequiredService(parameter.ParameterType);
                if (service == null) throw new InvalidOperationException($"Service {parameter.ParameterType.Name} not found.");
                parameterObjects.Add(service);
            }
            
            var endpoint = Activator.CreateInstance(item, parameterObjects.ToArray()) as CustomEndpoint<T>;
            if(endpoint is null) throw new InvalidOperationException($"Could not create instance of {item.Name}.");
            endpoints.Add(endpoint);
        }

        return endpoints;
    }
}