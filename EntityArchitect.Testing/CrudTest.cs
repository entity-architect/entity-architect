using System.Runtime.CompilerServices;
using System.Text;
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
        var testDataString = await File.ReadAllTextAsync(path!);

        var testModelType = typeof(object);
        var isMultiTest = attribute.AttributeType ==
                          typeof(MultiTestAttribute<>).MakeGenericType(
                              attribute.AttributeType.GetGenericArguments()[0]);

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
            var requestData = jsonObject["request".ToCamelCase()]?.ToString() ?? null;

            var modelData = JsonConvert.DeserializeObject<TestModelData>(model.ToString() ?? "{}");
            var testName = modelData?.TestName;
            if (testName is null)
                throw new Exception("Test name is null");
            switch (modelData?.Method)
            {
                case "POST":
                {
                    if (requestData is null)
                        throw new Exception("Request is null");
                    requestData.FormatRequest(responses);
                    var result = await Post(client, modelData, requestData);
                    responses.Add(new ValueTuple<string, object>(testName ,result));
                    break;
                }
                case "GET":
                {
                    var id = jsonObject["id"]?.ToString();
                    if (id is null)
                        throw new Exception("Id is null");
                    id = id.FormatRequest(responses);

                    if (model is not JObject formattedModel)
                        throw new Exception("Model is not JObject");
                    formattedModel["id"] = id;
                    var getModelData = JsonConvert.DeserializeObject<TestModelGet>(formattedModel.ToString()!);
                    if (getModelData is null)
                        throw new Exception("Model is not ModelTestGet");
                    var result = await Get(client, getModelData);
                    responses.Add(new ValueTuple<string, object>(testName, result));
                    break;
                }
                case "PUT":
                {
                    requestData = requestData.FormatRequest(responses);
                    if (requestData is null)
                        throw new Exception("Request is null");
                    var result = await Put(client, modelData, requestData);
                    responses.Add(new ValueTuple<string, object>(testName, result));
                    break;
                }
                case "DELETE":
                {
                    var id = jsonObject["id"]?.ToString();
                    if (id is null)
                        throw new Exception("Id is null");
                    id = id.FormatRequest(responses);

                    if (model is not JObject formattedModel)
                        throw new Exception("Model is not JObject");
                    formattedModel["id"] = id;
                    var getModelData = JsonConvert.DeserializeObject<TestModelGet>(formattedModel.ToString()!);
                    if (getModelData is null)
                        throw new Exception("Model is not ModelTestGet");
                    
                    await Delete(client, getModelData);
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


    private static async Task<string> Post(HttpClient client, TestModelData testModel, string requestData)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(httpMessage);

        var statusCodeResponse = response.EnsureSuccessStatusCode();

        Assert.Equal(testModel.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> Put(HttpClient client, TestModelData testModel, string requestData)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();

        Assert.Equal(testModel.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task Delete(HttpClient client, TestModelGet testModel)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/{testModel.Id}"),
        };
        var response = await client.SendAsync(httpMessage);
        var statusCodeResponse = response.EnsureSuccessStatusCode();

        Assert.Equal(testModel.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
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
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(testModel.ExpectedStatusCode, (int)statusCodeResponse.StatusCode);
        return content;
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