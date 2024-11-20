using System.Text.Json.Nodes;

namespace EntityArchitect.Testing.Helpers;

public static class RequestFormatter
{
   public static void FormatRequest(this string request, List<(string testName, object response)> responses)
   {
      if (string.IsNullOrEmpty(request) || responses == null || !responses.Any())
         return;

      var regex = new System.Text.RegularExpressions.Regex(@"{(\w+)\.(\w+)}");

      request =  regex.Replace(request, match =>
      {
         var testName = match.Groups[1].Value;
         var propertyName = match.Groups[2].Value;

         var matchingResponse = responses.FirstOrDefault(r => r.testName == testName);
         if (matchingResponse.response == null)
            return match.Value;
         var property = matchingResponse.response.GetType().GetProperty(propertyName);
         if (property == null)
            return match.Value; 
         var value = property.GetValue(matchingResponse.response);
         return value?.ToString() ?? match.Value; 
      });
   }

}