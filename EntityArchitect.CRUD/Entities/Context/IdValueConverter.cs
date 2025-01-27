using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntityArchitect.CRUD.Entities.Context;

public class IdValueConverter<TEntity>() : ValueConverter<Id<TEntity>, Guid>(
    id => id.Value,
    guid => new Id<TEntity>(guid))
    where TEntity : Entity;