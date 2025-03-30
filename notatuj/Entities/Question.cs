using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace notatuj.Entities;

[CannotUpdate, CannotGetById, CannotDelete, CannotCreate]
public class Question : Entity
{
    public string Content { get; set; }
    
    [OneToMany<Exam>(nameof(Exam.Question))]
    public Exam Exam { get; set; }
    
    [OneToMany<Answer>(nameof(Entities.Answer.Question))]
    public ICollection<Answer> Answers { get; set; }
}