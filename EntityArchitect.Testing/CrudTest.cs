using System.Runtime.CompilerServices;
using System.Text;
using EntityArchitect.CRUD.TypeBuilders;
using EntityArchitect.Testing.Helpers;
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
        List<(string testName, object response)> responses = [];
        if (responses == null) throw new ArgumentNullException(nameof(responses));

        var type = typeof(T);
        var method = type.GetMethod(methodName);

        var attribute = method!.CustomAttributes.First(c => c.AttributeType.BaseType == typeof(BaseTestAttribute));
        var path = attribute.ConstructorArguments[0].Value as string;
        TypeBuilder typeBuilder = new();
        var postRequestType = typeBuilder.BuildCreateRequestFromEntity(attribute.AttributeType.GetGenericArguments()[0]);
        var putRequestType = typeBuilder.BuildUpdateRequestFromEntity(attribute.AttributeType.GetGenericArguments()[0]);

        var testDataString = await File.ReadAllTextAsync(path!);

        var testModelType = typeof(TestModel<object>);
        bool isMultiTest = attribute.AttributeType ==
                           typeof(MultiTestAttribute<>).MakeGenericType(
                               attribute.AttributeType.GetGenericArguments()[0]);

        //TODO antoher request for put
        if (isMultiTest)
        {
            testModelType = typeof(List<>).MakeGenericType(testModelType);
        }

        var testModel = JsonConvert.DeserializeObject(testDataString, testModelType);
        if (testModel == null)
            throw new Exception("Test data is not json");

        List<object> testModels;
        if (isMultiTest)
        {
            testModels = (testModel as IEnumerable<object>)?.ToList() ??
                         throw new Exception("Failed to cast to List<object>");
        }
        else
        {
            testModels = [testModel];
        }

        foreach (var model in testModels)
        {
            var jsonRequest = JsonConvert.SerializeObject(model);
            var jsonObject = JObject.Parse(jsonRequest);
            var requestData = (jsonObject["Request"] ?? throw new Exception("Request data is missing")).ToString();

            requestData = requestData.Replace("Guid:Random", Guid.NewGuid().ToString());
            requestData = requestData.Replace("\"Int:Random\"", new Random().NextInt64().ToString());
            requestData = requestData.Replace("DateTime:Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var testName = (model as TestModelData)!.TestName;
            switch ((model as TestModelData)?.Method)
            {
                case "POST":
                {
                    requestData.FormatRequest(responses);
                    var result = await Post(client, model, requestData);
                    responses.Add(new ValueTuple<string, object>(testName ,result));
                    break;
                }
                case "GET":
                {
                    var result = await Get(client, model as TestModelGet);
                    responses.Add(new ValueTuple<string, object>(testName, result));
                    break;
                }
                case "PUT":
                {
                    requestData.FormatRequest(responses);
                    var result = await Put(client, model, requestData);
                    responses.Add(new ValueTuple<string, object>(testName, result));
                    break;
                }
                case "DELETE":
                {
                    await Delete(client, model, requestData);
                    break;
                }
                case "PAGINATED_GET":
                {
                    var result =  await PaginatedGet(client, (model as TestModelPaginatedGet)!);
                    responses.Add(new ValueTuple<string, object>(testName, result));
                    break;
                }
                default:
                    throw new InvalidOperationException("Invalid method type");
            }
        }
    }


    private static async Task<string> Post(HttpClient client, object testModel, string requestData)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{(testModel as TestModelData)!.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(httpMessage);

        var statusCodeResponse = response.EnsureSuccessStatusCode();

        Assert.Equal((testModel as TestModelData)!.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> Put(HttpClient client, object testModel, string requestData)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{(testModel as TestModelData)!.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();

        Assert.Equal((testModel as TestModelData)!.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task Delete(HttpClient client, object testModel, string requestData)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{(testModel as TestModelData)!.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();

        Assert.Equal((testModel as TestModelData)!.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
    }

    private static async Task<string> Get(HttpClient client, TestModelGet? testModel)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel!.EntityName.ToLower()}/{testModel.Id}"),
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();

        Assert.Equal(testModel.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> PaginatedGet(HttpClient client, TestModelPaginatedGet testModel)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/?page={testModel.Page}"),
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();

        Assert.Equal(testModel.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }
}