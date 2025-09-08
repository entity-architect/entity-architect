using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EntityArchitect.CRUD.CustomEndpoints;

public static class FeatureBuilder
{
    public static List<Feature<T>> Build<T>(Assembly assembly, IServiceScope serviceProvider) where T : Entity
    {
        var types = assembly.GetTypes().Where(c => c.BaseType == typeof(Feature<T>)).ToList();

        List<Feature<T>> endpoints = new();
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
            
            var endpoint = Activator.CreateInstance(item, parameterObjects.ToArray()) as Feature<T>;
            if(endpoint is null) throw new InvalidOperationException($"Could not create instance of {item.Name}.");
            endpoints.Add(endpoint);
        }

        return endpoints;
    }
}