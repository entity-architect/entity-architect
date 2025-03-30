using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace notatuj.Entities;

[CannotGetById]
public class Note : Entity
{
    public string Title { get; set; }
    public string Description { get; set; }
    
    [ManyToOne<Tag>(nameof(Tag.Note)), IgnorePutRequest, IgnorePostRequest]
    public ICollection<Tag> Tags { get; set; }
    
    [ManyToOne<Contribution>(nameof(Contribution.Note)), IgnorePutRequest, IgnorePostRequest]
    public ICollection<Contribution> Contributors { get; set; }
    
    [ManyToOne<Likes>(nameof(Entities.Likes.Note)), IgnorePutRequest, IgnorePostRequest]
    public ICollection<Likes> Likes { get; set; }
    
    [ManyToOne<Section>(nameof(Section.Note)), IgnorePutRequest, IgnorePostRequest]
    public ICollection<Section> Sections { get; set; }
    
    [ManyToOne<Exam>(nameof(Exam.Note)), IgnorePutRequest, IgnorePostRequest]
    public ICollection<Exam> Exams { get; set; }
}