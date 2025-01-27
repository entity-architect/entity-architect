using EntityArchitect.CRUD.Results.Abstracts;

namespace EntityArchitect.CRUD.Testing.TestModels;

public class TestResult
{
    public Result<object> Value { get; set; }
}

public class TestModelDoubleValue
{
    public TestResult Value { get; set; }
}

public class TestModelWithoutValue
{
    public ResultModel Value { get; set; }
}