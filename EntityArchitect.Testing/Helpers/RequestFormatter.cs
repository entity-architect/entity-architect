using System.Text.Json;
using System.Text.Json.Nodes;
using EntityArchitect.CRUD.TypeBuilders;
using EntityArchitect.Testing.TestModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = System.Text.Json.JsonException;

namespace EntityArchitect.Testing.Helpers;

public static class RequestFormatter
{
    public static string? FormatRequest(this string? request,
        List<(string testName, string response, Type entityType)> responses)
    {
        if (string.IsNullOrEmpty(request))
            return request;

        var regex = new System.Text.RegularExpressions.Regex(@"{(\w+)\.(.+?)(?=})}");

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


            TypeBuilder typeBuilder = new();
            var responseType = typeBuilder.BuildResponseFromEntity(matchingResponse.entityType);
            var currentNode = JsonConvert.DeserializeObject(matchingResponse.response, responseType);
            if (currentNode == null)
                return match.Value;
            foreach (var propertyName in propertyPath.Split('.'))
            {
                currentNode = currentNode.GetType().GetProperty(propertyName).GetValue(currentNode);
                if (currentNode is not null)
                    request = request.Replace("{" + testName + "." + propertyName + "}", currentNode.ToString());
            }

            return currentNode?.ToString() ?? match.Value;

        });

        return request;
    }

    public static TestModelGet? FormatGetRequest(this string? request, List<(string testName, string response, Type entityType)> responses, Type responseType)
    {
        if (string.IsNullOrEmpty(request) || responses.Count == 0)
            return null;

        var regex = new System.Text.RegularExpressions.Regex(@"{(\w+)\.(.+?)(?=})}");
        
        request = regex.Replace(request, match =>
        {
            var testName = match.Groups[1].Value;
            var propertyPath = match.Groups[2].Value;

            var matchingResponse = responses.FirstOrDefault(r => r.testName == testName);
            if (matchingResponse.response == null)
                return match.Value;

            var currentNode = JsonConvert.DeserializeObject(matchingResponse.response, responseType);
            if (currentNode == null)
                return match.Value;
            foreach (var propertyName in propertyPath.Split('.'))
            {
                currentNode = currentNode.GetType().GetProperty(propertyName).GetValue(currentNode);
                if (currentNode is not null)
                    request = request.Replace(testName + "." + propertyName, currentNode.ToString());
            }

            return currentNode?.ToString() ?? match.Value;

        });
        
        return request == null ? null : JsonConvert.DeserializeObject<TestModelGet>(request);
    }
 
}