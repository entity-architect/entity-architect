using System.Text.Json;
using System.Text.Json.Nodes;

namespace EntityArchitect.Testing.Helpers;

public static class RequestFormatter
{
    public static string? FormatRequest(this string? request, List<(string testName, object response)> responses)
    {
        if (string.IsNullOrEmpty(request) || responses.Count == 0)
            return request;

        var regex = new System.Text.RegularExpressions.Regex(@"{(\w+)\.(.+)}");

        request = request.Replace("Guid:Random", Guid.NewGuid().ToString());
        request = request.Replace("\"Int:Random\"", new Random().NextInt64().ToString());
        request = request.Replace("DateTime:Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        
        request = regex.Replace(request, match =>
        {
            var testName = match.Groups[1].Value; 
            var propertyPath = match.Groups[2].Value; 

            var matchingResponse = responses.FirstOrDefault(r => r.testName == testName);
            if (matchingResponse.response == null)
                return match.Value;

            try
            {
                var jsonString = matchingResponse.response as string;
                if (string.IsNullOrEmpty(jsonString))
                    return match.Value;

                var jsonObject = JsonNode.Parse(jsonString);
                if (jsonObject == null)
                    return match.Value;

                jsonObject = jsonObject["value"]?["value"] ?? jsonObject["value"];

                if (jsonObject == null)
                    return match.Value;
                
                var currentNode = jsonObject;
                foreach (var propertyName in propertyPath.Split('.'))
                {
                    currentNode = jsonObject?[ToCamelCase(propertyName)];
                    if (currentNode is not null)
                        request = request.Replace(testName + "." + "testName", currentNode.ToString());
                }

                return currentNode?.ToString() ?? match.Value;
            }
            catch (JsonException)
            {
                return match.Value;
            }
        });
        
        return request;
    }

    internal static string ToCamelCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var words = input
            .Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            return string.Empty;

        var camelCase = words[0].ToLowerInvariant();
        for (int i = 1; i < words.Length; i++)
        {
            camelCase += char.ToUpperInvariant(words[i][0]) + words[i].Substring(1).ToLowerInvariant();
        }

        return camelCase;
    }
}