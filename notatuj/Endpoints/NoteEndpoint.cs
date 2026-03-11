using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.CustomEndpoints;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Results.Abstracts;
using Microsoft.AspNetCore.Mvc;
using notatuj.Entities;
using notatuj.Services;

namespace notatuj.Endpoints;

public class NoteEndpoint(IRepository<Note> noteRepository, IRepository<Tag> tagRepository, IOpenAiApi service, IUnitOfWork unitOfWork, IRepository<Question> questionRepository, IRepository<Answer> asnswerRepository, IRepository<Exam> examRepository) : CustomEndpoint<Note>
{
    [CustomEndpoint("POST", "tag"), Secured(typeof(User))]
    public async Task<Result> AddTagToNote([FromBody] NoteRequest request, CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(request.NoteId, [nameof(Note.Sections), nameof(Note.Tags)], cancellationToken);
        if(note is null)
            return Result.Failure(Error.NotFound(request.NoteId, nameof(Note)));
        
        var tags = await service.GetTags(note.Sections.Select(s => s.Content).ToArray());

        foreach (var tag in note.Tags)
        {
            tagRepository.Remove(tag);
        }
        
        foreach (var tag in tags)
        {
            await tagRepository.AddAsync(new Tag() {TagName = tag, Note = note}, cancellationToken);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
    
    [CustomEndpoint("POST", "exam"), Secured(typeof(User))]
    public async Task<Result> GenerateExam([FromBody] ExamRequest request, CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(request.NoteId, [nameof(Note.Sections)], cancellationToken);
        if(note is null)
            return Result.Failure(Error.NotFound(request.NoteId, nameof(Note)));
        
        var exam = await service.GetExam(note.Sections.Select(s => s.Content).ToArray(), request.Count);
        
        var examId = Guid.NewGuid();
        var examEntity = Entity.CreateFromId<Exam>(examId);
        examEntity.Note = note;
        examEntity.Title = exam.Title;
        await examRepository.AddAsync(examEntity, cancellationToken);

        foreach (var question in exam.Questions)
        {
            var questionId = Guid.NewGuid();
            var questionModel = Entity.CreateFromId<Question>(questionId);
            questionModel.Content = question.Content;
            questionModel.Exam = examEntity;
            
            await questionRepository.AddAsync(questionModel, cancellationToken);
            
            foreach (var answer in question.Answers)
            {
                var answerId = Guid.NewGuid();
                var answerModel = Entity.CreateFromId<Answer>(answerId);
                answerModel.Content = answer.Content;
                answerModel.IsCorrect = answer.IsCorrect;
                answerModel.Question = questionModel;
                
                await asnswerRepository.AddAsync(answerModel, cancellationToken);
            }
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}

public class NoteRequest
{
    public Guid NoteId { get; set; }
}

public class ExamRequest
{
    public Guid NoteId { get; set; }
    public int Count { get; set; }
}