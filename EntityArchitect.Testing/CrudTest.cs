using System.Runtime.CompilerServices;
using System.Text;
using EntityArchitect.CRUD.TypeBuilders;
using EntityArchitect.Entities.Entities;
using EntityArchitect.Results;
using EntityArchitect.Results.Abstracts;
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
        List<(string testName, string response, Type entityType)> responses = [];
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

        var assembly = attribute.AttributeType.GetGenericArguments()[0].Assembly.GetTypes().Where(c => c.BaseType == typeof(Entity)).ToList();
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
            testModels = (testModel as IEnumerable<object>)?.ToList() ?? throw new Exception("Failed to cast to List<object>");
        }
        else
        {
            testModels = [testModel];
        }

        foreach (var model in testModels)
        {
            var jsonModel = JsonConvert.SerializeObject(model);
            if(jsonModel is null)
                throw new Exception("Failed to serialize model");
            TypeBuilder typeBuilder = new();
            var modelData = JsonConvert.DeserializeObject<TestModelData>(jsonModel);
            if (modelData == null) throw new Exception("Model data is null");
            var testName = modelData.TestName;
            if (testName is null)
                throw new Exception("Test name is null");
            
            var entity = assembly.FirstOrDefault(c => c.Name == modelData.EntityName);
            if (entity is null && modelData.Method != "ASSERT")
                throw new Exception("Entity is null");
            switch (modelData.Method)
            {
                case "POST":
                {
                    var requestType = typeBuilder.BuildCreateRequestFromEntity(entity);
                    jsonModel = jsonModel.FormatRequest(responses);

                    var testModelObject = JsonConvert.DeserializeObject(jsonModel, typeof(EndpointTestModel<>).MakeGenericType(requestType));
                    if (testModelObject is null)
                        throw new Exception("Failed to deserialize json object");
                    var requestData = JsonConvert.SerializeObject(testModelObject.GetType().GetProperty(nameof(EndpointTestModel<EntityRequest>.Request))?.GetValue(testModelObject));
                    if (requestData is null)
                        throw new Exception("Request is null");
                    
                    requestData.FormatRequest(responses);
                    var result = await Post(client, modelData, requestData);
                    responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));
                    break;
                }
                case "GET":
                {
                    var response = typeBuilder.BuildResponseFromEntity(typeof(Entity));
                    if(jsonModel is null)
                        throw new Exception("Failed to format request");
                    var testModelObject = jsonModel.FormatGetRequest(responses,response);
                    if (testModelObject is null)
                        throw new Exception("Failed to deserialize json object");

                    var result = await Get(client, testModelObject);
                    responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));
                    break;
                }
                case "PUT":
                {
                    var requestType = typeBuilder.BuildUpdateRequestFromEntity(entity);
                    jsonModel = jsonModel.FormatRequest(responses);

                    var testModelObject = JsonConvert.DeserializeObject(jsonModel, typeof(EndpointTestModel<>).MakeGenericType(requestType));
                    if (testModelObject is null)
                        throw new Exception("Failed to deserialize json object");
                    var requestData = JsonConvert.SerializeObject(testModelObject.GetType().GetProperty(nameof(EndpointTestModel<EntityRequest>.Request))?.GetValue(testModelObject));
                    if (requestData is null)
                        throw new Exception("Request is null");
                    
                    var result = await Put(client, modelData, requestData);
                    responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));
                    break;
                }
                case "DELETE":
                {
                    var response = typeBuilder.BuildResponseFromEntity(typeof(Entity));
                    if(jsonModel is null)
                        throw new Exception("Failed to format request");
                    var testModelObject = jsonModel.FormatGetRequest(responses, response);
                    if (testModelObject is null)
                        throw new Exception("Failed to deserialize json object");
                    
                    await Delete(client, testModelObject);
                    break;
                }
                case "PAGINATED_GET":
                {
                    var responseType = typeBuilder.BuildResponseFromEntity(entity);
                    
                    if(jsonModel is null)
                        throw new Exception("Failed to format request");
                    if (JsonConvert.DeserializeObject(jsonModel, 
                            typeof(TestModelPaginatedGet)) 
                        is not TestModelPaginatedGet testModelObject)
                        throw new Exception("Failed to deserialize json object");

                    var entityResponseType = typeBuilder.BuildResponseFromEntity(entity);
                    var result =  await PaginatedGet(client, testModelObject, entityResponseType);
                    result = ExtractContent(result);
                    var getResponse = JsonConvert.DeserializeObject(result, typeof(PaginatedResult<>).MakeGenericType(responseType));
                    if(getResponse is null)
                        throw new Exception("Failed to deserialize response");
                    
                    Assert.Equal(testModelObject.ExceptedTotalElementCount, getResponse.GetType().GetProperty(nameof(PaginatedResult<EntityResponse>.TotalElementCount))?.GetValue(getResponse));
                    responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));
                    break;
                }
                case "ASSERT":
                {
                    jsonModel = jsonModel.FormatRequest(responses);
                    
                    if (JsonConvert.DeserializeObject(jsonModel, 
                            typeof(AssertModel)) 
                        is not AssertModel assertModel)
                        throw new Exception("Failed to deserialize json object");

                    switch (assertModel.Operation)
                    {
                        case "EQUAL":
                        {
                            var componentA = assertModel.ComponentA;
                            var componentB = assertModel.ComponentB;
                            Assert.Equal(componentA, componentB);
                            break;
                        }
                        case "NOT_EQUAL":
                        {
                            var componentA = assertModel.ComponentA;
                            var componentB = assertModel.ComponentB;
                            Assert.NotEqual(componentA, componentB);
                            break;
                        }
                        default:
                            throw new InvalidOperationException("Invalid method type");
                    }
                    
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
        var content = await response.Content.ReadAsStringAsync();
        ValidateResponse(content, testModel.ExpectedStatusCode == 200);
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
        var content = await response.Content.ReadAsStringAsync();
        ValidateResponse(content, testModel.ExpectedStatusCode == 200);

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
        var content = await response.Content.ReadAsStringAsync();
        
        ValidateResponse(content, testModel.ExpectedStatusCode == 200);
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
        var content = await response.Content.ReadAsStringAsync();
        ValidateResponse(content, testModel.ExpectedStatusCode == 200);
        return content;
    }

    private static async Task<string> PaginatedGet(HttpClient client, TestModelPaginatedGet testModel, Type entityResponseType)
    {
        var url = $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/list/{testModel.Page}";
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri =
                new Uri(url),
        };
        
        
        var response = await client.SendAsync(httpMessage);
        var content = await response.Content.ReadAsStringAsync();
        content = ExtractContent(content);
        var paginatedResponse = JsonConvert.DeserializeObject(content, typeof(PaginatedResult<>).MakeGenericType(entityResponseType));
        paginatedResponse = paginatedResponse.GetType().GetProperty(nameof(PaginatedResult<EntityResponse>.TotalElementCount))?.GetValue(paginatedResponse);
        Assert.Equal(testModel.ExceptedTotalElementCount, paginatedResponse);
        return await response.Content.ReadAsStringAsync();
    }
    
    private static void ValidateResponse(string response, bool isSuccess = true)
    {
        Result? result = null;
        if (JsonConvert.DeserializeObject(response)?.GetType().GetProperty(nameof(Result<object>.Value)) is null)
        {
            var singleValueWithoutValueResult = JsonConvert.DeserializeObject<TestModelWithoutValue?>(response);
            Assert.Equal(singleValueWithoutValueResult is not null && singleValueWithoutValueResult.Value.IsSuccess, isSuccess);
            return;
        }

        var singleValueResult = JsonConvert.DeserializeObject<TestResult?>(response); 
        if(singleValueResult is not null)
            result = singleValueResult.Value;
        else
        {
            var doubleValueResult = JsonConvert.DeserializeObject<TestModelDoubleValue>(response);
            if(doubleValueResult is not null)
                result = doubleValueResult.Value.Value;
        }
                               
        Assert.Equal(result is not null && result.IsSuccess, isSuccess);
    }
    
    private static string ExtractContent(string json)
    {
        try
        {
            var jsonObject = JObject.Parse(json);
            var value = jsonObject["value"];
            if (value is JObject valueObject && valueObject["value"] != null)
            {
                return valueObject["value"].ToString();
            }

            if (value != null)
            {
                return value.ToString();
            }

            throw new Exception("Failed to extract content");
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}