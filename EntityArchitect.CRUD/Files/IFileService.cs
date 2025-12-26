using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Results.Abstracts;
using Microsoft.AspNetCore.Http;

namespace EntityArchitect.CRUD.Files;

public interface IFileService
{
    Task<Result> UploadFileAsync(IFormFile fileStream, EntityFile entityFile, string path, MinFileAttribute? minFile, CancellationToken cancellationToken);
    Task<Result> DeleteFileAsync(EntityFile entityFile, string path, CancellationToken cancellationToken);
    Task<Result<Stream>> DownloadFileAsync(EntityFile entityFile, string path, CancellationToken cancellationToken);
}