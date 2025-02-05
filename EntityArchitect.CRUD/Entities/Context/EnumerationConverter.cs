using EntityArchitect.CRUD.Enumerations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntityArchitect.CRUD.Entities.Context;

public class EnumerationConverter<TEnumeration>() : ValueConverter<TEnumeration, int>(c => c.Id, c => Enumeration.GetById<TEnumeration>(c)) where TEnumeration : Enumeration;