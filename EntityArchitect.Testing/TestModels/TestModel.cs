namespace EntityArchitect.Testing.TestModels;

public class TestModel<TRequest> : TestModelData
{
    public TRequest Request { get; set; }
}