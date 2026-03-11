using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace notatuj.Entities;

[CannotGetById]
public class Section : Entity
{
    [OneToMany<Note>(nameof(Note.Sections)), IgnorePutRequest]
    public Note Note { get; set; }
    
    public string Content { get; set; }
}