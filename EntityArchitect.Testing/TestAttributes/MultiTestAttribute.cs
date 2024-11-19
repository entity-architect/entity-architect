using System.Runtime.CompilerServices;
using EntityArchitect.Entities.Entities;
using EntityArchitect.Testing.TestModels;
using Newtonsoft.Json;

namespace EntityArchitect.Testing.TestAttributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MultiTestAttribute<TEntity> : BaseTestAttribute where TEntity : Entity
{
    public MultiTestAttribute(string testDataFileName, [CallerMemberName] string methodName = "")
    {
        if (!File.Exists(testDataFileName))
            throw new Exception("Test file does not exist");
        
        var testDataString = File.ReadAllText(testDataFileName);
        var testModels = JsonConvert.DeserializeObject<List<TestModelData>>(testDataString);
        if (testModels == null)
            throw new Exception("Test data is not json");

        if (testModels.GetType() != typeof(List<TestModelData>))
            throw new Exception("Test data is null");

        if (testModels.Count == 0)
            throw new Exception("Test data is empty");

        var modelData = testModels.First();
        DisplayName = modelData.TestName;
    }
}