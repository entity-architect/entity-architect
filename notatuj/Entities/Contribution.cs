using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using notatuj.Enumerations;

namespace notatuj.Entities;

[CannotUpdate, CannotGetById]
public class Contribution : Entity
{
    [OneToMany<Note>(nameof(Entities.Note.Contributors))] public Note Note { get; set; }
    [OneToMany<User>(nameof(Entities.User.Notes))] public User User { get; set; }
    [IgnorePostRequest, IgnorePutRequest] public ContributorType ContributorType { get; set; }
}