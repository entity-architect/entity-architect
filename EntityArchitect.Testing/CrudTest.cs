using System.Runtime.CompilerServices;
using System.Text;
using EntityArchitect.CRUD.TypeBuilders;
using EntityArchitect.Testing.TestAttributes;
using EntityArchitect.Testing.TestModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace EntityArchitect.Testing;

public static class CrudTest
{
    [Fact]
    public static async Task RunTest<T>(this HttpClient client, [CallerMemberName] string methodName = "")
    {
        var type = typeof(T);
        var method = type.GetMethod(methodName);

        var attribute = method!.CustomAttributes.First(c => c.AttributeType.BaseType == typeof(BaseTestAttribute));
        var path = attribute.ConstructorArguments[0].Value as string;
        TypeBuilder typeBuilder = new();
        var requestType = typeBuilder.BuildCreateRequestFromEntity(attribute.AttributeType.GetGenericArguments()[0]);

        var testDataString = await File.ReadAllTextAsync(path!);

        var testModelType = typeof(TestModel<>).MakeGenericType(requestType);
        if (attribute.AttributeType == typeof(MultiTestAttribute<>).MakeGenericType(attribute.AttributeType.GetGenericArguments()[0]) )
        {
            testModelType = typeof(List<>).MakeGenericType(testModelType);
        }

        var testModel = JsonConvert.DeserializeObject(testDataString, testModelType);
        if (testModel == null)
            throw new Exception("Test data is not json");

        List<object> testModels;
        if (attribute.AttributeType == typeof(MultiTestAttribute<>).MakeGenericType(attribute.AttributeType.GetGenericArguments()[0]))
        {
            testModels = (testModel as List<object>)!;
        }
        else
        {
            testModels = [(TestModelData)testModel];
        }

        foreach (var model in testModels)
        {
            var jsonRequest = JsonConvert.SerializeObject(model);
            var jsonObject = JObject.Parse(jsonRequest);
            var requestData = (jsonObject["Request"] ?? jsonObject["Request"]!).ToString();

            switch ((model as TestModelData).Method)
            {
                case "POST":
                    await Post(client, model, requestData);
                    break;
                case "GET":
                    await Get(client, model as TestModelGet);
                    break;
                case "PUT":
                    await Put(client, model, requestData);
                    break;
                case "DELETE":
                    await Delete(client, model, requestData);
                    break;
                case "PAGINATED_GET":
                    await PaginatedGet(client, (model as TestModelPaginatedGet)!);
                    break;
            }
        }
    }

    private static async Task Post(HttpClient client, object testModel, string requestData)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{(testModel as TestModelData)!.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();
        
        Assert.Equal((testModel as TestModelData)!.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
    }
    
    private static async Task Put(HttpClient client, object testModel, string requestData)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            RequestUri = new Uri($"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{(testModel as TestModelData)!.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();
        
        Assert.Equal((testModel as TestModelData)!.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
    }
    
    private static async Task Delete(HttpClient client, object testModel, string requestData)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri($"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{(testModel as TestModelData)!.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();
        
        Assert.Equal((testModel as TestModelData)!.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
    }

    private static async Task Get(HttpClient client, TestModelGet? testModel)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel!.EntityName.ToLower()}/{testModel.Id}"),
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();
        
        Assert.Equal(testModel.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
    }
    
    private static async Task PaginatedGet(HttpClient client, TestModelPaginatedGet testModel)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel   .EntityName.ToLower()}/?page={testModel.Page}"),
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();
        
        Assert.Equal(testModel.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
    }
}