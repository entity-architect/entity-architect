using System.Reflection;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntityArchitect.Entities.Context;

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
                    .HasForeignKey((relation.NamedArguments.First(c => c.MemberName == "ForeignKey").TypedValue.Value as string)!);
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
        }

        return modelBuilder;
    }
}