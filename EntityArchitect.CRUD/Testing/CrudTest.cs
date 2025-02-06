using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Results;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Testing.Exceptions;
using EntityArchitect.CRUD.Testing.Helpers;
using EntityArchitect.CRUD.Testing.TestAttributes;
using EntityArchitect.CRUD.Testing.TestModels;
using EntityArchitect.CRUD.TypeBuilders;
using Microsoft.AspNetCore.Identity.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace EntityArchitect.CRUD.Testing;

public static class CrudTest
{
    public static async Task RunTest<T>(this HttpClient client, [CallerMemberName] string methodName = "")
    {
        List<ReportModel> reports = new();
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

        var assembly = attribute.AttributeType.GetGenericArguments()[0].Assembly.GetTypes()
            .Where(c => c.BaseType == typeof(Entity)).ToList();
        if (isMultiTest) testModelType = typeof(List<>).MakeGenericType(testModelType);

        var testModel = JsonConvert.DeserializeObject(testDataString, testModelType);
        if (testModel == null)
            throw new Exception("Test data is not json");

        var generateReport = false;
        List<object> testModels;
        if (isMultiTest)
        {
            generateReport = (bool) method!.CustomAttributes.First(c => c.AttributeType == typeof(MultiTestAttribute<>)
                .MakeGenericType(attribute.AttributeType.GetGenericArguments()[0])).ConstructorArguments[1].Value!;
            testModels = (testModel as IEnumerable<object>)?.ToList() ??
                         throw new Exception("Failed to cast to List<object>");
        }
        else
            testModels = [testModel];

        foreach (var model in testModels)
        {
            try
            {
                var jsonModel = JsonConvert.SerializeObject(model);
                if (jsonModel is null)
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

                        var testModelObject = JsonConvert.DeserializeObject(jsonModel,
                            typeof(EndpointTestModel<>).MakeGenericType(requestType));
                        if (testModelObject is null)
                            throw new Exception("Failed to deserialize json object");
                        var requestData = JsonConvert.SerializeObject(testModelObject.GetType()
                            .GetProperty(nameof(EndpointTestModel<EntityRequest>.Request))?.GetValue(testModelObject));
                        var authorizationToken = JsonConvert.SerializeObject(testModelObject.GetType()
                            .GetProperty(nameof(EndpointTestModel<EntityRequest>.AuthorizationToken))?.GetValue(testModelObject)); 
                        
                        if (requestData is null)
                            throw new Exception("Request is null");

                        requestData.FormatRequest(responses);
                        var result = await Post(client, modelData, requestData,authorizationToken);
                        responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));
                        reports.WriteReport(testName, ExtractContent(result), requestData, entity, "POST");

                        break;
                    }
                    case "GET":
                    {
                        var response = typeBuilder.BuildResponseFromEntity(typeof(Entity));
                        if (jsonModel is null)
                            throw new Exception("Failed to format request");
                        var testModelObject = jsonModel.FormatGetRequest(responses, response);
                        
                        if (testModelObject is null)
                            throw new Exception("Failed to deserialize json object");
                        var authorizationToken = JsonConvert.SerializeObject(testModelObject.GetType()
                            .GetProperty(nameof(EndpointTestModel<EntityRequest>.AuthorizationToken))?.GetValue(testModelObject)); 


                        var result = await Get(client, testModelObject, authorizationToken);
                        responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));

                        reports.WriteReport(testName, ExtractContent(result), testModelObject.Id.ToString(), entity,
                            "GET");
                        break;
                    }
                    case "PUT":
                    {
                        var requestType = typeBuilder.BuildUpdateRequestFromEntity(entity);
                        jsonModel = jsonModel.FormatRequest(responses);

                        var testModelObject = JsonConvert.DeserializeObject(jsonModel,
                            typeof(EndpointTestModel<>).MakeGenericType(requestType));
                        if (testModelObject is null)
                            throw new Exception("Failed to deserialize json object");
                        var requestData = JsonConvert.SerializeObject(testModelObject.GetType()
                            .GetProperty(nameof(EndpointTestModel<EntityRequest>.Request))?.GetValue(testModelObject));
                        if (requestData is null)
                            throw new Exception("Request is null");
                        
                        var authorizationToken = JsonConvert.SerializeObject(testModelObject.GetType()
                            .GetProperty(nameof(EndpointTestModel<EntityRequest>.AuthorizationToken))?.GetValue(testModelObject)); 
                        

                        var result = await Put(client, modelData, requestData, authorizationToken);
                        responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));

                        reports.WriteReport(testName, ExtractContent(result), requestData, entity, "PUT");
                        break;
                    }
                    case "DELETE":
                    {
                        var response = typeBuilder.BuildResponseFromEntity(typeof(Entity));
                        if (jsonModel is null)
                            throw new Exception("Failed to format request");
                        var testModelObject = jsonModel.FormatGetRequest(responses, response);
                        if (testModelObject is null)
                            throw new Exception("Failed to deserialize json object");
                                                
                        var authorizationToken = JsonConvert.SerializeObject(testModelObject.GetType()
                            .GetProperty(nameof(EndpointTestModel<EntityRequest>.AuthorizationToken))?.GetValue(testModelObject)); 
                        
                        await Delete(client, testModelObject, authorizationToken);

                        reports.WriteReport(testName, "Deleted", testModelObject.Id.ToString(), entity, "DELETE");
                        break;
                    }
                    case "PAGINATED_GET":
                    {
                        var responseType = typeBuilder.BuildResponseFromEntity(entity);

                        if (jsonModel is null)
                            throw new Exception("Failed to format request");
                        if (JsonConvert.DeserializeObject(jsonModel,
                                typeof(TestModelPaginatedGet))
                            is not TestModelPaginatedGet testModelObject)
                            throw new Exception("Failed to deserialize json object");

                        var entityResponseType = typeBuilder.BuildResponseFromEntity(entity);
                        var authorizationToken = JsonConvert.SerializeObject(testModelObject.GetType()
                            .GetProperty(nameof(EndpointTestModel<EntityRequest>.AuthorizationToken))?.GetValue(testModelObject)); 

                        var result = await PaginatedGet(client, testModelObject, entityResponseType, authorizationToken);
                        result = ExtractContent(result);
                        var getResponse = JsonConvert.DeserializeObject(result,
                            typeof(PaginatedResult<>).MakeGenericType(responseType));
                        if (getResponse is null)
                            throw new Exception("Failed to deserialize response");

                        Assert.Equal(testModelObject.ExceptedTotalElementCount,
                            getResponse.GetType().GetProperty(nameof(PaginatedResult<EntityResponse>.TotalElementCount))
                                ?.GetValue(getResponse));
                        responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));

                        reports.WriteReport(testName, ExtractContent(result), testModelObject.Page.ToString(), entity,
                            "PAGINATED_GET");
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

                        reports.WriteReport(testName, "Asserted", "", typeof(Entity), "ASSERT");

                        break;
                    }
                    case "LOGIN":
                    {
                        var requestType = typeof(Authorization.Requests.AuthorizationRequest);
                        jsonModel = jsonModel.FormatRequest(responses);

                        var testModelObject = JsonConvert.DeserializeObject(jsonModel,
                            typeof(EndpointTestModel<>).MakeGenericType(requestType));
                        if (testModelObject is null)
                            throw new Exception("Failed to deserialize json object");
                        var requestData = JsonConvert.SerializeObject(testModelObject.GetType()
                            .GetProperty(nameof(EndpointTestModel<EntityRequest>.Request))?.GetValue(testModelObject));
                        if (requestData is null)
                            throw new Exception("Request is null");

                        requestData.FormatRequest(responses);
                        var result = await Login(client, requestData, modelData.EntityName.ToLower());
                        responses.Add(new ValueTuple<string, string, Type>(testName, ExtractContent(result), entity));
                        reports.WriteReport(testName, ExtractContent(result), requestData, entity, "POST");

                        break;
                    }
                    default:
                        throw new InvalidOperationException("Invalid method type");
                }
            }
            catch (Exception e)
            {
                var jsonModel = JsonConvert.SerializeObject(model);
                if (jsonModel is null)
                    throw new Exception("Failed to serialize model");
                var modelData = JsonConvert.DeserializeObject<TestModelData>(jsonModel);
                if (modelData is null) throw new Exception("Model data is null");
                throw new TestException(e, modelData.TestName);
            }
        }

        if (generateReport)
        {
            var report = JsonConvert.SerializeObject(reports);
            await File.WriteAllTextAsync("report.json", report);
        }
    }

    private static void WriteReport(this List<ReportModel> models, string testName, string response, string requestBody, Type entityType, string method)
    {
        var reportModel = new ReportModel
        {
            Id = models.Count + 1,
            Name= testName,
            Response = response,
            Date = DateTime.Now,
            Body = requestBody,
            Method = method,
            EntityName = entityType.Name
        };
        models.Add(reportModel);
    }

    private static async Task<string> Post(HttpClient client, TestModelData testModel, string requestData, string? authorizationToken)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json"),
        };
        
        if(testModel.AuthorizationToken is not null)
            httpMessage.Headers.Add("Authorization", ("Bearer " + authorizationToken).Replace("\"", ""));
        
        var response = await client.SendAsync(httpMessage);
        var content = await response.Content.ReadAsStringAsync();
        ValidateResponse(content, testModel.TestName, testModel.ExpectedStatusCode == 200);
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> Put(HttpClient client, TestModelData testModel, string requestData, string? authorizationToken)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/"),
            Content = new StringContent(requestData, Encoding.UTF8, "application/json"),
        };
        if(testModel.AuthorizationToken is not null)
            httpMessage.Headers.Add("Authorization", ("Bearer " + authorizationToken).Replace("\"", ""));
        
        var response = await client.SendAsync(httpMessage);
        var content = await response.Content.ReadAsStringAsync();
        ValidateResponse(content, testModel.TestName,testModel.ExpectedStatusCode == 200);

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task Delete(HttpClient client, TestModelGet testModel, string? authorizationToken)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/{testModel.Id}")
        };
        if(testModel.AuthorizationToken is not null)
            httpMessage.Headers.Add("Authorization", ("Bearer " + authorizationToken).Replace("\"", ""));

        var response = await client.SendAsync(httpMessage);
        var content = await response.Content.ReadAsStringAsync();

        ValidateResponse(content, testModel.TestName,testModel.ExpectedStatusCode == 200);
    }

    private static async Task<string> Get(HttpClient client, TestModelGet? testModel, string? authorizationToken)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel!.EntityName.ToLower()}/{testModel.Id}")
        };
        if(testModel.AuthorizationToken is not null)
            httpMessage.Headers.Add("Authorization", ("Bearer " + authorizationToken).Replace("\"", ""));

        var response = await client.SendAsync(httpMessage);
        var content = await response.Content.ReadAsStringAsync();
        ValidateResponse(content, testModel.TestName,testModel.ExpectedStatusCode == 200);
        return content;
    }
    
    private static async Task<string> Login(HttpClient client, string request, string entityName)
    {
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri =
                new Uri(
                    $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{entityName}/login"),
            Content = new StringContent(request, Encoding.UTF8, "application/json"),
        };
      
        var response = await client.SendAsync(httpMessage);
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    private static async Task<string> PaginatedGet(HttpClient client, TestModelPaginatedGet testModel,
        Type entityResponseType, string? authorizationToken)
    {
        var url =
            $"{client.BaseAddress!.Scheme}://{client.BaseAddress.Host}/{testModel.EntityName.ToLower()}/list/{testModel.Page}";
        var httpMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url)
        };
        if(testModel.AuthorizationToken is not null)
            httpMessage.Headers.Add("Authorization", ("Bearer " + authorizationToken).Replace("\"", ""));
        
        var response = await client.SendAsync(httpMessage);
        var content = await response.Content.ReadAsStringAsync();
        content = ExtractContent(content);
        var paginatedResponse =
            JsonConvert.DeserializeObject(content, typeof(PaginatedResult<>).MakeGenericType(entityResponseType));
        paginatedResponse = paginatedResponse.GetType()
            .GetProperty(nameof(PaginatedResult<EntityResponse>.TotalElementCount))?.GetValue(paginatedResponse);
        Assert.Equal(testModel.ExceptedTotalElementCount, paginatedResponse);
        return await response.Content.ReadAsStringAsync();
    }

    private static void ValidateResponse(string response, string testName, bool isSuccess = true)
    {
        Result? result = null;
        if (JsonConvert.DeserializeObject(response)?.GetType().GetProperty(nameof(Result<object>.Value)) is null 
            || JsonConvert.DeserializeObject(response)?.GetType().GetProperty(nameof(Result<object>.Value)).GetValue(JsonConvert.DeserializeObject(response)) is null)
        {
            var singleValueWithoutValueResult = JsonConvert.DeserializeObject<Result>(response);
            if(singleValueWithoutValueResult is not null && singleValueWithoutValueResult.Errors.Count == 0 != isSuccess)
                throw new TestException(new Exception("Bad result."), testName);
            Assert.Equal(singleValueWithoutValueResult is not null && singleValueWithoutValueResult.Errors.Count == 0, isSuccess);
            return;
        }

        var singleValueResult = JsonConvert.DeserializeObject<TestResult?>(response);
        if (singleValueResult is not null)
        {
            result = singleValueResult.Value;
        }
        else
        {
            var doubleValueResult = JsonConvert.DeserializeObject<TestModelDoubleValue>(response);
            if (doubleValueResult is not null)
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
            if (value is JObject valueObject && valueObject["value"] != null) return valueObject["value"].ToString();

            if (value != null) return value.ToString();

            throw new Exception("Failed to extract content");
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}