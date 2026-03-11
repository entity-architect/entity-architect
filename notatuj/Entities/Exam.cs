using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace notatuj.Entities;

[CannotGetById]
public class Exam : Entity
{
    public string Title { get; set; }
    
    [OneToMany<Note>(nameof(Note.Exams))]
    public Note Note { get; set; }
    
    [ManyToOne<Question>(nameof(Entities.Question.Exam)), IgnorePutRequest, IgnorePostRequest]
    public ICollection<Question> Question { get; set; }
}