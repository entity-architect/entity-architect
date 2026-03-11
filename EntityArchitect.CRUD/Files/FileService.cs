using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Results.Abstracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace EntityArchitect.CRUD.Files;

public class FileService(IConfiguration configuration) : IFileService
{
    public async Task<Result> UploadFileAsync(IFormFile fileStream, EntityFile entityFile, string path, MinFileAttribute? minFile, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Ftp").Get<Ftp>();
        if (section is null)
            return Result.Failure(new Error(HttpStatusCode.InternalServerError,
                "Ftp section is not found in appsettings.json"));

        var fileServer = $"{section.Protocol}://{section.Host}:{section.Port}";

        if (minFile is not null)
        {
            try
            {
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                using var image = await Image.LoadAsync(memoryStream, cancellationToken);
                
                var minStream = new MemoryStream();
                
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(minFile.X, minFile.Y),
                    Mode = ResizeMode.Max 
                }));
                
                if (fileStream.FileName.Split('.').Last().ToLower() == "png")
                {
                    await image.SaveAsync(minStream, new PngEncoder()
                    {
                        CompressionLevel = PngCompressionLevel.Level6
                    }, cancellationToken);
                }
                else if(fileStream.FileName.Split('.').Last().ToLower() == "jpg" || fileStream.FileName.Split('.').Last().ToLower() == "jpeg")
                {
                    await image.SaveAsync(minStream, new JpegEncoder
                    {
                        Quality = 70
                    }, cancellationToken);
                }
                else if(fileStream.FileName.Split('.').Last().ToLower() == "webp")
                {
                    await image.SaveAsWebpAsync(minStream, new WebpEncoder()
                    {
                        Quality = 70
                    }, cancellationToken);
                }
                else if(fileStream.FileName.Split('.').Last().ToLower() == "gif"){
                    await image.SaveAsGifAsync(minStream, new GifEncoder()
                    {
                        ColorTableMode = GifColorTableMode.Global,
                        Quantizer = new SixLabors.ImageSharp.Processing.Processors.Quantization.OctreeQuantizer()
                    }, cancellationToken);
                }
                else if (fileStream.FileName.Split('.').Last().ToLower() == "bmp")
                {
                    await image.SaveAsBmpAsync(minStream, new BmpEncoder()
                    {
                        BitsPerPixel = BmpBitsPerPixel.Pixel32
                    }, cancellationToken);
                }
                else if(fileStream.FileName.Split('.').Last().ToLower() == "tif")
                {
                    await image.SaveAsTiffAsync(minStream, new TiffEncoder()
                    {
                        BitsPerPixel = TiffBitsPerPixel.Bit32
                    }, cancellationToken);
                }
                else
                {
                    return Result.Failure(new Error(HttpStatusCode.Conflict,
                        "File type is not supported"));
                }
                minStream.Position = 0;
                
                var fileMinLocation = Path.Combine(section.Root, path, "min", entityFile.Id + entityFile.Extension);
                var uploadMinUrl = fileServer + fileMinLocation;
                Console.WriteLine(uploadMinUrl);

                var requestMin = (FtpWebRequest)WebRequest.Create(uploadMinUrl);
                requestMin.Method = WebRequestMethods.Ftp.UploadFile;
                requestMin.Credentials = new NetworkCredential(section.Login, section.Password);
                requestMin.UseBinary = true;
                requestMin.UsePassive = true;
                requestMin.KeepAlive = false;

                await using (var requestStream = await requestMin.GetRequestStreamAsync())
                {
                    await minStream.CopyToAsync(requestStream, cancellationToken);
                }

                using (var response = (FtpWebResponse)await requestMin.GetResponseAsync())
                {
                    Console.WriteLine($"Upload status: {response.StatusDescription}");
                }
            }
            catch (Exception e)
            {
                return Result.Failure(new Error(HttpStatusCode.InternalServerError,
                    "Failed to create min file: " + e.Message));
            }
        }
        
        
        
        var fileLocation = Path.Combine(section.Root, path, entityFile.Id + entityFile.Extension);
        var uploadUrl = fileServer + fileLocation;
        Console.WriteLine(uploadUrl);

        var request = (FtpWebRequest)WebRequest.Create(uploadUrl);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(section.Login, section.Password);
        request.UseBinary = true;
        request.UsePassive = true;
        request.KeepAlive = false;

        await using (var requestStream = await request.GetRequestStreamAsync())
        {
            await fileStream.OpenReadStream().CopyToAsync(requestStream, cancellationToken);
        }

        using (var response = (FtpWebResponse)await request.GetResponseAsync())
        {
            Console.WriteLine($"Upload status: {response.StatusDescription}");
            return Result.Success();
        }
    }

    public async Task<Result> DeleteFileAsync(EntityFile entityFile, string path, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Ftp").Get<Ftp>();
        if (section is null)
            return Result.Failure(new Error(HttpStatusCode.InternalServerError,
                "Ftp section is not found in appsettings.json"));
        var fileLocation = section.Root + path + "/" + entityFile.Id + entityFile.Extension;
        var fileServer = $"{section.Protocol}://{section.Host}:{section.Port}";

        try
        {
            var ftpWebRequest = (FtpWebRequest)WebRequest.Create(fileServer + fileLocation);
            ftpWebRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            ftpWebRequest.Credentials = new NetworkCredential(section.Login, section.Password);
            ftpWebRequest.UseBinary = true;
            ftpWebRequest.UsePassive = section.UsePassive;
            ftpWebRequest.KeepAlive = false;
            ftpWebRequest.Timeout = section.Timeout;
        
            using var response = await ftpWebRequest.GetResponseAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

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