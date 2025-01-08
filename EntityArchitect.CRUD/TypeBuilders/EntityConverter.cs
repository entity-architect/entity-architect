using System.Collections;
using System.Reflection;
using EntityArchitect.CRUD.Authorization;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD.TypeBuilders;

public static class EntityConverter
{
    public static TEntity ConvertRequestToEntity<TEntity, TRequest>(this TRequest requestInstance)
        where TEntity : Entity
    {
        var entityInstance = Activator.CreateInstance(typeof(TEntity)) as TEntity;
        
        var entityProperties = typeof(TEntity).GetProperties();
        var requestProperties = requestInstance!.GetType().GetProperties();

        foreach (var propertyEntity in entityProperties)
        {
            var propertyRequest = Array.Find(requestProperties,
                p => p.Name == propertyEntity.Name || p.Name == propertyEntity.Name + "Id");
            if (propertyRequest == null || !propertyRequest.CanRead) continue;
            var value = propertyRequest.GetValue(requestInstance);

            if (propertyEntity.PropertyType.BaseType == typeof(Entity))
            {
                var attributeType = typeof(RelationOneToManyAttribute<>)
                    .MakeGenericType(propertyEntity.PropertyType);

                if (propertyEntity.CustomAttributes.Select(c => c.AttributeType).Contains(attributeType))
                {
                    var subEntityInstance = Activator.CreateInstance(propertyEntity.PropertyType);
                    propertyEntity.PropertyType.GetProperty(nameof(Entity.Id))
                        ?.SetValue(subEntityInstance, new Id<Entity>((Guid)value!).ToId());
                    propertyEntity.SetValue(entityInstance, subEntityInstance);
                    continue;
                }
            }

            propertyEntity.SetValue(entityInstance,
                propertyRequest.Name == "Id" ? new Id<TEntity>((Guid)value!).ToId() : value);
        }

        entityInstance.HashPassword();
        return entityInstance!;
    }
    public static TResponse ConvertEntityToResponse<TEntity, TResponse>(this TEntity entityInstance)
        where TEntity : Entity
        where TResponse : EntityResponse, new()
    {
        var responseInstance = Activator.CreateInstance<TResponse>();
        var responseProperties = responseInstance.GetType().GetProperties();
        var entityProperties = entityInstance.GetType().GetProperties();

        foreach (var propertyEntity in responseProperties)
        {
            var propertyResponse = Array.Find(entityProperties, p => p.Name == propertyEntity.Name);
            if (propertyResponse == null || !propertyResponse.CanRead) continue;
            var value = propertyResponse.GetValue(entityInstance);
            
            if (propertyEntity.PropertyType.BaseType == typeof(EntityResponse) ||
                (propertyEntity.PropertyType.IsGenericType &&
                 propertyEntity.PropertyType.GetGenericArguments()[0].BaseType == typeof(EntityResponse)))
            {
                switch (value)
                {
                    case null when propertyEntity.PropertyType.IsGenericType &&
                                   propertyEntity.PropertyType.GetGenericArguments()[0].BaseType ==
                                   typeof(EntityResponse):
                    {
                        var emptyListType =
                            typeof(List<>).MakeGenericType(propertyEntity.PropertyType.GetGenericArguments()[0]);
                        var emptyList = Activator.CreateInstance(emptyListType);
                        propertyEntity.SetValue(responseInstance,emptyList);
                        continue;
                    }
                    case null:
                        propertyEntity.SetValue(responseInstance, null);
                        continue;
                }
                
                if(value.GetType().IsGenericType && value.GetType().GetGenericArguments()[0].BaseType == typeof(Entity))
                {
                    var listType = typeof(List<>).MakeGenericType(propertyEntity.PropertyType.GetGenericArguments()[0]);
                    var list = Activator.CreateInstance(listType) as IList;
                    foreach (var item in (value as IList)!)
                    {
                        var listValue = typeof(EntityConverter).GetMethod(nameof(ConvertEntityToResponse))!
                            .MakeGenericMethod(item.GetType(), propertyEntity.PropertyType.GetGenericArguments()[0])//TODO: get response type
                            .Invoke(null, new[] { item });
                        list!.Add(listValue);
                    }
                    
                    propertyEntity.SetValue(responseInstance, list);
                    continue;
                }
                
                var convertedValue = typeof(EntityConverter).GetMethod(nameof(ConvertEntityToResponse))!
                    .MakeGenericMethod(value.GetType(), propertyEntity.PropertyType)
                    .Invoke(null, new[] { value });
                propertyEntity.SetValue(responseInstance, convertedValue);
                continue;
            }

            if (propertyResponse.Name == nameof(Entity.Id))
            {
                propertyEntity.SetValue(responseInstance, (value as Id<Entity>)!.ToId().Value);
                continue;
            }

            propertyEntity.SetValue(responseInstance, value);
        }

        return responseInstance!;
    }
    public static TResponse ConvertEntityToLightListResponse<TEntity, TResponse>(this TEntity entityInstance)
    {
        var responseInstance = Activator.CreateInstance<TResponse>();
        var responseProperties = typeof(TResponse).GetProperties();
        var entityProperties = entityInstance!.GetType().GetProperties();

        foreach (var propertyEntity in responseProperties)
        {
            if(propertyEntity.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationPasswordAttribute)))
                continue;
            
            var propertyResponse = Array.Find(entityProperties, p => p.Name == propertyEntity.Name);
            if (propertyResponse == null || !propertyResponse.CanRead) continue;
            var value = propertyResponse.GetValue(entityInstance);
            if (propertyResponse.Name == nameof(Entity.Id))
            {
                propertyEntity.SetValue(responseInstance, (value as Id<Entity>)!.ToId().Value);
                continue;
            }
            
            propertyEntity.SetValue(responseInstance, value);
        }
        
        return responseInstance;
    }
    private static void HashPassword<TEntity>(this TEntity entityInstance) where TEntity : Entity?
    {
        if (typeof(TEntity).GetCustomAttribute<AuthorizationEntityAttribute>() is null)
            return;
        
        var passwordProperty = typeof(TEntity)
            .GetProperties().FirstOrDefault(c =>
                c.CustomAttributes
                    .Any(x => x.AttributeType ==
                              typeof(AuthorizationPasswordAttribute)));

        if (passwordProperty is null) return;
        
        var password = typeof(TEntity).GetProperty(passwordProperty.Name)!
            .GetValue(entityInstance)!.ToString();
                
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        passwordProperty.SetValue(entityInstance, hashedPassword);
    }
}