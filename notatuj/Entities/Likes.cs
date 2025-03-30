using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace notatuj.Entities;

[CannotUpdate]
[CannotGetById]
public class Likes : Entity
{
    [OneToMany<Note>(nameof(Note.Likes))]
    public Note Note { get; set; }
    
    [OneToMany<User>(nameof(User.Likes))]
    public User User { get; set; }
}