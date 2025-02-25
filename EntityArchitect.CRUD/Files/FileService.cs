using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Results.Abstracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EntityArchitect.CRUD.Files;

public class FileService(IConfiguration configuration) : IFileService
{
    public async Task<Result> UploadFileAsync(IFormFile fileStream, EntityFile entityFile, string path, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Ftp").Get<Ftp>();
        if (section is null)
            return Result.Failure(new Error(HttpStatusCode.InternalServerError,
                "Ftp section is not found in appsettings.json"));

        var fileLocation = Path.Combine(section.Root, path, entityFile.Id + entityFile.Extension);
        var fileServer = $"{section.Protocol}://{section.Host}:{section.Port}";
        var streamPath = Path.Combine(fileServer, fileLocation);
        Console.WriteLine(fileServer + fileLocation);
        var ftpWebRequest = (FtpWebRequest)WebRequest.Create(streamPath);
        ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
        ftpWebRequest.Credentials = new NetworkCredential(section.Login, section.Password);
        ftpWebRequest.UseBinary = true;
        ftpWebRequest.Timeout = section.Timeout;

        await using var requestStream = await ftpWebRequest.GetRequestStreamAsync();
        await fileStream.CopyToAsync(requestStream, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteFileAsync(EntityFile entityFile, string path, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Ftp").Get<Ftp>();
        if (section is null)
            return Result.Failure(new Error(HttpStatusCode.InternalServerError,
                "Ftp section is not found in appsettings.json"));

        var fileLocation = section.Root + path + "/" + entityFile.Id + entityFile.Extension;
        var fileServer = $"{section.Protocol}://{section.Host}:{section.Port}";

        var ftpWebRequest = (FtpWebRequest)WebRequest.Create(fileServer + fileLocation);
        ftpWebRequest.Method = WebRequestMethods.Ftp.DeleteFile;
        ftpWebRequest.Credentials = new NetworkCredential(section.Login, section.Password);
        ftpWebRequest.UseBinary = true;
        ftpWebRequest.Timeout = section.Timeout;

        using var response = await ftpWebRequest.GetResponseAsync();


        return Result.Success();
    }

    public async Task<Result<Stream>> DownloadFileAsync(EntityFile entityFile, string path,
        CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Ftp").Get<Ftp>();
        if (section is null)
            return Result.Failure<Stream?>(new Error(HttpStatusCode.InternalServerError,
                "Ftp section is not found in appsettings.json"));

        var fileLocation = section.Root + path + "/" + entityFile.Id + entityFile.Extension;
        var fileServer = $"{section.Protocol}://{section.Host}:{section.Port}";

        var ftpWebRequest = (FtpWebRequest)WebRequest.Create(fileServer + fileLocation);
        ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;
        ftpWebRequest.Credentials = new NetworkCredential(section.Login, section.Password);
        ftpWebRequest.UseBinary = true;
        ftpWebRequest.UsePassive = true;

        ftpWebRequest.Timeout = -1;
        ftpWebRequest.ReadWriteTimeout = -1;
        Stopwatch sw = new();
        sw.Start();
        using var ftpWebResponse = (FtpWebResponse)await ftpWebRequest.GetResponseAsync();
        await using var responseStream = ftpWebResponse.GetResponseStream();
        Console.WriteLine(sw.ElapsedMilliseconds);

        var memoryStream = new MemoryStream();
        await responseStream.CopyToAsync(memoryStream).ConfigureAwait(false);
        memoryStream.Position = 0;
        Console.WriteLine(sw.ElapsedMilliseconds);
        if (responseStream is null)
            return Result.Failure<Stream>(new Error(HttpStatusCode.InternalServerError,
                "Failed to retrieve stream from FTP response"));

        return Result.Success<Stream>(memoryStream);
    }
}