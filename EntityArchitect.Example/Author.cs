using System.Runtime.CompilerServices;
using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.Queries;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;
using ILogger = EntityArchitect.Example.Services.Logger.ILogger;

namespace EntityArchitect.Example;

[HasLightList]
[GetListPaginated(3)]
public class Author : Entity
{
    [LightListProperty]
    public string Name { get; private set; }

    [RelationManyToOne<Book>(nameof(Book.Author)), IgnorePostRequest, IgnorePutRequest, IncludeInGet(1)]
    public List<Book> Books { get; private set; }
    public void AddToName(string addedByAction)
    {
        Name += addedByAction;
    }
}
public class AuthorSearchQuery() : Query<Author>("SELECT id, name FROM author WHERE name LIKE @AuthorName:STRING:2 ORDER BY name ASC;");
public class AuthorCountQuery() : Query<Author>("sql/count.sql", true);
public class AuthorCreateAction(ILogger logger) : EndpointAction<Author>
{
    protected override ValueTask<Author> BeforePostAsync(Author entity, CancellationToken cancellationToken = default)
    {
        entity.AddToName(" added by action");
        logger.Log("Some logic before post");
        return base.BeforePostAsync(entity, cancellationToken);
    }
}