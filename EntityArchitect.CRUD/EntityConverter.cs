using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD;

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

            propertyEntity.SetValue(entityInstance,
                propertyRequest.Name == "Id" ? new Id<TEntity>((Guid)value!).ToId() : value);
        }

        return entityInstance!;
    }
    public static TResponse ConvertEntityToResponse<TEntity, TResponse>(this TEntity entityInstance)
        where TEntity : Entity
    {
        var responseInstance = Activator.CreateInstance<TResponse>();
        var responseProperties = typeof(TResponse).GetProperties();
        var entityProperties = entityInstance.GetType().GetProperties();

        foreach (var propertyEntity in responseProperties)
        {
            var propertyResponse = Array.Find(entityProperties, p => p.Name == propertyEntity.Name);
            if (propertyResponse == null || !propertyResponse.CanRead) continue;
            var value = propertyResponse.GetValue(entityInstance);

            if (propertyEntity.PropertyType.BaseType == typeof(Entity))
            {
                var convertedValue = typeof(EntityConverter).GetMethod(nameof(ConvertEntityToResponse))!
                    .MakeGenericMethod(propertyEntity.PropertyType, propertyResponse.PropertyType)
                    .Invoke(null, new object[] { entityInstance});
                propertyEntity.SetValue(responseInstance, convertedValue);
            }

            if (propertyEntity.PropertyType.BaseType == typeof(EntityResponse))
            {
                var convertedValue = typeof(EntityConverter).GetMethod(nameof(ConvertEntityToResponse))!
                    .MakeGenericMethod(value!.GetType(), propertyEntity.PropertyType)
                    .Invoke(null, new object[] { value });

                propertyEntity.SetValue(responseInstance, convertedValue);
                continue;
            }

            propertyEntity.SetValue(responseInstance,
                propertyResponse.Name == "Id" ? (value as Id<Entity>)!.Value : value);
        }

        return responseInstance!;
    }
}