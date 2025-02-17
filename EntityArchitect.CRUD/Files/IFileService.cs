using EntityArchitect.CRUD.Results.Abstracts;

namespace EntityArchitect.CRUD.Files;

public interface IFileService
{
    Task<Result> UploadFileAsync(IFormFile fileStream, File file, CancellationToken cancellationToken);
    Task<Result> DeleteFileAsync(File file, CancellationToken cancellationToken);
}