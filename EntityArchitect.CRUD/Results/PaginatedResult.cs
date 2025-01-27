using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Results;

public class PaginatedResult<TResponse>(List<TResponse> results, int page, int leftPages, int pageCount, int totalCount)
    where TResponse : EntityResponse
{
    public int Page { get; set; } = page;
    public int LeftPages { get; set; } = leftPages;
    public int PageCount { get; set; } = pageCount;
    public int TotalElementCount { get; set; } = totalCount;
    public bool PreviousPage => Page > 0;
    public bool NextPage => LeftPages > 0;
    public bool AnyResults => Results.Count > 0;

    public List<TResponse> Results { get; set; } = results;
}