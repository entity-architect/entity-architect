using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Example;

public class Book : Entity
{
    public string? Title { get; private set; }
    
    [RelationOneToMany<Author>(nameof(Author.Books))]
    public Author Author { get;private set; }
}