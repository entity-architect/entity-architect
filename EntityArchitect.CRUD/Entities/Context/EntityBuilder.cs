using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using EFCore.NamingConventions.Internal;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Enumerations;
using EntityArchitect.CRUD.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntityArchitect.CRUD.Entities.Context;

public static class EntityBuilder
{
    public static ModelBuilder BuildEntity(this ModelBuilder modelBuilder, Type entity)
    {
        var idProperty = entity.GetProperty("Id");

        if (!idProperty.PropertyType.IsGenericType ||
            idProperty.PropertyType.GetGenericTypeDefinition() != typeof(Id<>)) return modelBuilder;

        var genericArgument = idProperty.PropertyType.GetGenericArguments()[0];
        var converterType = typeof(IdValueConverter<>).MakeGenericType(genericArgument);
        var converter = (ValueConverter)Activator.CreateInstance(converterType)!;

        modelBuilder.Entity(entity)
            .Property(idProperty.Name)
            .HasConversion(converter);

        var properties = entity.GetProperties();

        foreach (var property in properties)
        {
            if (property.PropertyType.BaseType == typeof(Entity))
            {
                var attributeOneToManyType = typeof(OneToManyAttribute<>).MakeGenericType(property.PropertyType);
                var attributeOneToOneType = typeof(OneToOneAttribute<>).MakeGenericType(property.PropertyType);

                if (property.PropertyType.BaseType == typeof(ValueObject))
                {
                    modelBuilder.Entity(entity).OwnsOne(property.PropertyType, property.Name);
                    continue;
                }
                
                if (property.CustomAttributes.Select(c => c.AttributeType)
                    .Contains(attributeOneToManyType))
                {
                    var relationType = property.CustomAttributes
                        .First(c => c.AttributeType == attributeOneToManyType)
                        .AttributeType.GetGenericArguments()[0];

                    if (relationType is null) continue;
                    var relation = property.CustomAttributes
                        .First(c => c.AttributeType == attributeOneToManyType);
                    var fk = relation.ConstructorArguments.First().Value as string;
                    
                    modelBuilder.Entity(entity)
                        .HasOne(property.Name)
                        .WithMany(fk)  
                        .HasForeignKey(property.Name + "Id");
                }
                else if (property.CustomAttributes.Select(c => c.AttributeType)
                         .Contains(attributeOneToOneType))
                {
                    var relationType = property.CustomAttributes
                        .First(c => c.AttributeType == attributeOneToOneType)
                        .AttributeType.GetGenericArguments()[0];

                    if (relationType is null) continue;
                    var relation = property.CustomAttributes
                        .First(c => c.AttributeType == attributeOneToOneType);
                    var fk = relation.ConstructorArguments.First().Value as string;

                    var hasForeignKeyMethods = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.ReferenceReferenceBuilder).GetMethods();
                    var hasForeignKeyMethod = hasForeignKeyMethods.First(m => m.Name == "HasForeignKey" && m.GetParameters()[0].ParameterType == typeof(Type));
                    hasForeignKeyMethod.Invoke(modelBuilder.Entity(entity).HasOne(property.Name).WithOne(fk), new object[] { property.PropertyType, new[] { fk + "Id"}, });
                    modelBuilder.Entity(entity).Property<Id<Entity>>(property.Name + "Id");
                    modelBuilder.Entity(entity).Property(property.Name + "Id").HasConversion(converter);
                }
            }
            else if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericArguments().First().BaseType == typeof(Entity))
            {
                var attributeManyToOneType =
                    typeof(ManyToOneAttribute<>).MakeGenericType(property.PropertyType.GetGenericArguments()
                        .First());

                if (property.CustomAttributes.Select(c => c.AttributeType)
                    .Contains(attributeManyToOneType))
                {
                    var relationType = property.CustomAttributes
                        .First(c => c.AttributeType == attributeManyToOneType)
                        .AttributeType.GetGenericArguments()[0];

                    if (relationType == null) continue;
                    var relation = property.CustomAttributes
                        .First(c => c.AttributeType == attributeManyToOneType);
                    var fk = relation.ConstructorArguments.First().Value as string;

                    modelBuilder.Entity(entity)
                        .HasMany(property.Name)
                        .WithOne(fk);
                    //.HasForeignKey(fk + "Id");
                }
            }
            else if (property.PropertyType.BaseType == typeof(Enumeration))
            {
                var enumerationConverterType = typeof(EnumerationConverter<>).MakeGenericType(property.PropertyType);
                var enumerationConverter = (ValueConverter)Activator.CreateInstance(enumerationConverterType)!;
                modelBuilder.Entity(entity).Property(property.Name).HasConversion(enumerationConverter);
            }
            else if (property.PropertyType == typeof(EntityFile))
            {
                if (property.CustomAttributes.All(c => c.AttributeType != typeof(EntityFileAttribute)))
                {
                    throw new Exception(
                        $"EntityFile property {property.Name} in entity {entity.Name} must have EntityFileAttribute.");
                }
                
                modelBuilder.Entity(entity)
                    .OwnsOne(property.PropertyType, property.Name);
            }
        }
        return modelBuilder;
    }
}
