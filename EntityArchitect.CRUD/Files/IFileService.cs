using EntityArchitect.CRUD.Results.Abstracts;

namespace EntityArchitect.CRUD.Files;

public interface IFileService
{
    Task<Result> UploadFileAsync(IFormFile fileStream, EntityFile entityFile, string path, CancellationToken cancellationToken);
    Task<Result> DeleteFileAsync(EntityFile entityFile, string path, CancellationToken cancellationToken);
    Result<string> GetOutputPath(EntityFile entityFile, string path);
}