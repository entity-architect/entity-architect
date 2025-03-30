using notatuj.Entities;
using notatuj.Repsonses;

namespace notatuj.Services;

public interface IOpenAiApi
{
    Task<string[]> GetTags(string[] sections);
    Task<ExamResponse> GetExam(string[] sections, int count);
}