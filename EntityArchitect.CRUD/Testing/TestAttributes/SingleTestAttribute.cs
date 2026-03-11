using System;
using System.IO;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Testing.TestModels;
using EntityArchitect.CRUD.TypeBuilders;
using Newtonsoft.Json;

namespace EntityArchitect.CRUD.Testing.TestAttributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SingleTestAttribute<TEntity> : BaseTestAttribute where TEntity : Entity
{
    public SingleTestAttribute(string testDataFileName)
    {
        if (!File.Exists(testDataFileName))
            throw new Exception("Test file does not exist");

        TypeBuilder typeBuilder = new();
        var requestType = typeBuilder.BuildCreateRequestFromEntity(typeof(TEntity));

        var testDataString = File.ReadAllText(testDataFileName);
        var testModelType = typeof(EndpointTestModel<>).MakeGenericType(requestType!);

        var testModel = JsonConvert.DeserializeObject(testDataString, testModelType);
        if (testModel == null)
            throw new Exception("Test data is not json");

        if (testModel.GetType() != testModelType)
            throw new Exception("Test data is null");

        var modelData = (TestModelData)testModel;

        DisplayName = modelData.TestName;
    }
}