using Newtonsoft.Json;
using notatuj.Entities;
using notatuj.Repsonses;
using OpenAI;
using OpenAI.Chat;

namespace notatuj.Services;

public class OpenAiService : IOpenAiApi
{
    private readonly ChatClient _client;

    public OpenAiService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"]!;
        _client = new ChatClient(model: "gpt-4o", apiKey);
    }

    public async Task<string[]> GetTags(string[] sections)
    {
        var prompt =
            "Analizuj poniższe sekcje notatki i zwróć listę tagów opisujących jej główne tematy w formacie JSON:\n\n";
        prompt += string.Join("\n", sections);
        prompt += "\n\nZwróć wynik w formacie JSON jako tablicę stringów.";

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(prompt)
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages);

        var responseContent = completion.Content[0].Text.Trim();
        responseContent =responseContent.Replace("```json", "");
        responseContent =responseContent.Replace("```", "");
        responseContent =responseContent.Replace("\n", "");
        try
        {
            return JsonConvert.DeserializeObject<string[]>(responseContent);
        }
        catch
        {
            return new string[] { "Błąd parsowania odpowiedzi OpenAI" };
        }
    }

    public async Task<ExamResponse> GetExam(string[] sections, int count)
    {
        string prompt = "Na podstawie poniższych sekcji notatek wygeneruj test składający się z " + count + " pytań. " +
                        "Każde pytanie powinno mieć jedną poprawną odpowiedź i kilka niepoprawnych. " +
                        "Odpowiedzi na pytania muszą być zawarte w podanych notatkach. " +
                        "Zwróć wynik w formacie JSON zgodnym z poniższymi modelami:\n\n" +
                        "Exam:\n{\n  \"Title\": \"string\",\n  \"Questions\": [\n    {\n      \"Content\": \"string\",\n      \"Answers\": [\n        {\n          \"Content\": \"string\",\n          \"IsCorrect\": true/false\n        }\n      ]\n    }\n  ]\n}\n\n" +
                        "Sekcje notatek:\n" + string.Join("\n", sections);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(prompt)
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages);
        var responseContent = completion.Content[0].Text.Trim();
        responseContent =responseContent.Replace("```json", "");
        responseContent =responseContent.Replace("```", "");
        responseContent =responseContent.Replace("\n", "");
        try
        {
            return JsonConvert.DeserializeObject<ExamResponse>(responseContent);
        }
        catch
        {
            throw new Exception("Błąd parsowania odpowiedzi OpenAI");
        }
    }
}