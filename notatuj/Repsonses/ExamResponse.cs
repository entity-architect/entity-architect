namespace notatuj.Repsonses;

public class ExamResponse
{
    public string Title { get; set; }
    public ICollection<QuestionResponse> Questions { get; set; }
}

public class QuestionResponse
{
    public string Content { get; set; }
    public ICollection<AnswerResponse> Answers { get; set; }
}

public class AnswerResponse
{
    public string Content { get; set; }
    public bool IsCorrect { get; set; }
}