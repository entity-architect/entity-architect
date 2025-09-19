using System.Reflection;

namespace EntityArchitect.CRUD.Feature;

public static class CommandBuilder
{
    public static ICollection<IBaseCommandHandler> Build(Assembly assembly, IServiceScope serviceProvider)
    {
        var types = assembly.GetTypes().Where(c => c.GetInterfaces()
            .Contains(typeof(IBaseCommand))).ToList();
        var handlers = assembly.GetTypes().Where(c => c.GetInterfaces()
            .Contains(typeof(IBaseCommandHandler)) && c is { IsInterface: false, IsAbstract: false }).ToList();
        List<IBaseCommandHandler> endpoints = [];
        foreach (var item in types)
        {
            var handlerType = handlers.FirstOrDefault(c => c.GetInterfaces().Skip(c.GetInterfaces().Length - 2).First().GetGenericArguments()[0] == item);
            if (handlerType == null) throw new InvalidOperationException($"Handler for {item.Name} not found.");
            var parameters = handlerType.GetConstructors().First().GetParameters();

            List<object> parameterObjects = [];
            foreach (var parameter in parameters)
            {
                var service = serviceProvider.ServiceProvider.GetRequiredService(parameter.ParameterType);
                if (service == null) throw new InvalidOperationException($"Service {parameter.ParameterType.Name} not found.");
                parameterObjects.Add(service);
            }

            var endpoint = Activator.CreateInstance(handlerType, parameterObjects.ToArray()) as IBaseCommandHandler;
            if(endpoint is null) throw new InvalidOperationException($"Could not create instance of {handlerType.Name}.");
            endpoints.Add(endpoint);
        }

        return endpoints;
    }
    
    
}