using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Testing.TestModels;

public class TestModel<TRequest> : TestModelData where TRequest : EntityRequest
{
    public TRequest Request { get; set; }
}