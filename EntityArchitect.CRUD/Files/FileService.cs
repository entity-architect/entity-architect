using System.Net;
using EntityArchitect.CRUD.Results.Abstracts;

namespace EntityArchitect.CRUD.Files;

public class FileService(IConfiguration configuration) : IFileService
{
    public async Task<Result> UploadFileAsync(IFormFile fileStream, File file, CancellationToken cancellationToken)
    {
        var section = configuration.Get<Ftp>();
        if (section is null)
            return Result.Failure(new Error(HttpStatusCode.InternalServerError,
                "Ftp section is not found in appsettings.json"));

        var fileLocation = section.Root + "/" + file.Id + file.Extension;
        var fileServer = $"{section.Protocol}://{section.Host}:{section.Port}";

        var ftpWebRequest = (FtpWebRequest)WebRequest.Create(fileServer + fileLocation);
        ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
        ftpWebRequest.Credentials = new NetworkCredential(section.Login, section.Password);
        ftpWebRequest.UseBinary = true;
        ftpWebRequest.Timeout = section.Timeout;

        await using var requestStream = await ftpWebRequest.GetRequestStreamAsync();
        await fileStream.CopyToAsync(requestStream, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteFileAsync(File file, CancellationToken cancellationToken)
    {
        var section = configuration.Get<Ftp>();
        if (section is null)
            return Result.Failure(new Error(HttpStatusCode.InternalServerError,
                "Ftp section is not found in appsettings.json"));

        var fileLocation = section.Root + "/" + file.Id + file.Extension;
        var fileServer = $"{section.Protocol}://{section.Host}:{section.Port}";

        var ftpWebRequest = (FtpWebRequest)WebRequest.Create(fileServer + fileLocation);
        ftpWebRequest.Method = WebRequestMethods.Ftp.DeleteFile;
        ftpWebRequest.Credentials = new NetworkCredential(section.Login, section.Password);
        ftpWebRequest.UseBinary = true;
        ftpWebRequest.Timeout = section.Timeout;

        using var response = await ftpWebRequest.GetResponseAsync();


        return Result.Success();
    }
}