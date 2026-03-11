using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace notatuj.Entities;

[CannotUpdate]
[CannotGetById]
public class Tag : Entity
{
    public string TagName { get; set; }
    
    [OneToMany<Note>(nameof(Entities.Note.Tags))]
    public Note Note { get; set; }
}