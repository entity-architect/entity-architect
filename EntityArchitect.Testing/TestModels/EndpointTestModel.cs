using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Testing.TestModels;

public class EndpointTestModel<TRequest> : TestModelData where TRequest : EntityRequest
{
    public TRequest Request { get; set; }
}