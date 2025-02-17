using System;
using System.Linq;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Enumerations;
using EntityArchitect.CRUD.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using File = EntityArchitect.CRUD.Files.File;

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
            if (property.PropertyType == typeof(EntityArchitect.CRUD.Files.File))
            {
                if (property.CustomAttributes.All(c => c.AttributeType != typeof(FilePathAttribute)))
                {
                    throw new Exception(
                        $"File property {property.Name} in entity {entity.Name} must have FilePathAttribute.");
                }
                
                modelBuilder.Entity(entity)
                    .OwnsOne(property.PropertyType, property.Name);
            }
            
            if (property.CustomAttributes.Select(c => c.AttributeType).Contains(typeof(RelationOneToManyAttribute<>)))
            {
                var relationType = property.CustomAttributes
                    .First(c => c.AttributeType == typeof(RelationOneToManyAttribute<>))
                    .ConstructorArguments[0].Value as Type;

                if (relationType == null) continue;

                var relation = property.CustomAttributes
                    .First(c => c.AttributeType == typeof(RelationOneToManyAttribute<>));

                modelBuilder.Entity(entity)
                    .HasMany(relationType)
                    .WithOne(property.Name)
                    .HasForeignKey(
                        (relation.NamedArguments.First(c => c.MemberName == "ForeignKey").TypedValue.Value as string)!);
            }
            else if (property.CustomAttributes.Select(c => c.AttributeType)
                     .Contains(typeof(RelationManyToOneAttribute<>)))
            {
                var relationType = property.CustomAttributes
                    .First(c => c.AttributeType == typeof(RelationManyToOneAttribute<>))
                    .ConstructorArguments[0].Value as Type;

                if (relationType == null) continue;

                modelBuilder.Entity(entity)
                    .HasOne(relationType)
                    .WithOne(property.Name)
                    .HasForeignKey(nameof(Entity.Id));
            }
            else
            {
                if (property.PropertyType.BaseType == typeof(Enumeration))
                {
                    var enumerationConverterType = typeof(EnumerationConverter<>).MakeGenericType(property.PropertyType);
                    var enumerationConverter = (ValueConverter)Activator.CreateInstance(enumerationConverterType)!;
                    modelBuilder.Entity(entity).Property(property.Name).HasConversion(enumerationConverter);
                }
            }
        }
        return modelBuilder;
    }
}