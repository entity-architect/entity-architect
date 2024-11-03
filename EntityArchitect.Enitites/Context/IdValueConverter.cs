using EntityArchitect.Entities.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntityArchitect.Entities.Context;

public class IdValueConverter<TEntity>() : ValueConverter<Id<TEntity>, Guid>(
    id => id.Value,
    guid => new Id<TEntity>(guid))
    where TEntity : Entity;