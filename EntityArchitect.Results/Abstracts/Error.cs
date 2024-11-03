using System.Net;

namespace EntityArchitect.Results.Abstracts;

public record Error(HttpStatusCode Code, string Message)
{
    public static Error None => new(HttpStatusCode.OK, string.Empty);
    public static Error NullValue => new(HttpStatusCode.BadRequest, "Value cannot be null.");
    public static Error NotFound(Guid id, string entityName) => new(HttpStatusCode.NotFound, $"Entity {entityName} with id {id} not found.");
}