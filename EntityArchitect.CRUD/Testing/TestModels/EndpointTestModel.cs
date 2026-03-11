using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Testing.TestModels;

public class EndpointTestModel<TRequest> : TestModelData where TRequest : EntityRequest
{
    public TRequest Request { get; set; }
}