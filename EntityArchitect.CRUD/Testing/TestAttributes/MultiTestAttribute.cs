using System.Runtime.CompilerServices;
using EntityArchitect.CRUD.Testing.TestModels;
using Newtonsoft.Json;

namespace EntityArchitect.CRUD.Testing.TestAttributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MultiTestAttribute<TEntity> : BaseTestAttribute
{
    public MultiTestAttribute(string testDataFileName, bool generateReport = false, [CallerMemberName] string methodName = "")
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