using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Results;

public class PaginatedResult<TResponse>(List<TResponse> results, int page, int leftPages, int pageCount)
    where TResponse : EntityResponse
{
    public int Page { get; set; } = page;
    public int LeftPages { get; set; } = leftPages;
    public int PageCount { get; set; } = pageCount;
    public bool NextPage => LeftPages > 0;
    
    public List<TResponse> Results { get; set; } = results;
}