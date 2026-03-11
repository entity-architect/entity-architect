using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace notatuj.Entities;

[CannotUpdate, CannotGetById, CannotDelete, CannotCreate]
public class Answer : Entity
{
    public string Content { get; set; }
    public bool IsCorrect { get; set; }
    
    [OneToMany<Question>(nameof(Question.Answers))]
    public Question Question { get; set; }
}