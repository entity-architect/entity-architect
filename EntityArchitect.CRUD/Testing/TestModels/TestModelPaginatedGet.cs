namespace EntityArchitect.CRUD.Testing.TestModels;

public class TestModelPaginatedGet : TestModelData
{
    public int Page { get; set; }
    public int ExceptedTotalElementCount { get; set; }
}