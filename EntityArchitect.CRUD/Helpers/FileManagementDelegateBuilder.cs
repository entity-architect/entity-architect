using System.Diagnostics;
using System.Net;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Files;
using EntityArchitect.CRUD.Results.Abstracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace EntityArchitect.CRUD.Helpers;

public class FileManagementDelegateBuilder<TEntity> where TEntity : Entity
{
    private readonly IServiceProvider _provider;
    private readonly string _field;
    private readonly string _path;
    private readonly string _entityName = typeof(TEntity).Name;
    private FileManagementDelegateBuilder(IServiceProvider provider, string field, string path)
    {
        _provider = provider;
        _field = field;
        _path = path;
    }

    public static FileManagementDelegateBuilder<TE> 
        Create<TE>(
            IServiceProvider provider,
            string field,
            string path)
        where TE : Entity
        => new(provider, field, path);
    
    public Func<Guid, IFormFile, CancellationToken, ValueTask<Result>> UploadFile =>
        async (entityId, file, cancellationToken) =>
        {
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var entity = await service.GetByIdAsync(entityId, null,cancellationToken);
                if (entity is null)
                    return Result.Failure(Error.NotFound(entityId, _entityName));

                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                if (entity.GetType().GetProperty(_field)!.GetValue(entity) is EntityFile oldEntityFile)
                {
                    await fileService.DeleteFileAsync(oldEntityFile, _path, cancellationToken);
                }
                
                var entityFile = EntityFile.Create(file);  
                
                var result = await fileService.UploadFileAsync(file, entityFile, _path, cancellationToken);
                if (result.IsFailure)
                    return result;

                entity.GetType().GetProperty(_field)!.SetValue(entity,entityFile);
                service.Update(entity);
                
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        };

    public Func<Guid, CancellationToken, ValueTask<Result>> DeleteFile =>
        async (entityId, cancellationToken) =>
        {
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var entity = await service.GetByIdAsync(entityId, null, cancellationToken);
                if (entity is null)
                    return Result.Failure(Error.NotFound(entityId, _entityName));

                if(entity.GetType().GetProperty(_field)!.GetValue(entity) is null)
                    return Result.Failure(new Error(HttpStatusCode.BadRequest, typeof(TEntity).Name + " " + _field + " not have a file."));

                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                var entityFile = (EntityFile)entity.GetType().GetProperty(_field)!.GetValue(entity)!;

                var result = await fileService.DeleteFileAsync(entityFile, _path, cancellationToken);
                if (result.IsFailure)
                    return result;

                entity.GetType().GetProperty(_field)!.SetValue(entity, null);
                service.Update(entity);

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }


            return Result.Success();
        };
    
    public Func<Guid, CancellationToken, Task<IResult>> DownloadFile =>
        async (entityId, cancellationToken) =>
        {


            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var entity = await service.GetByIdAsync(entityId, null, cancellationToken);
                if (entity is null)
                    return Microsoft.AspNetCore.Http.Results.NotFound();

                if(entity.GetType().GetProperty(_field)!.GetValue(entity) is null)
                    return Microsoft.AspNetCore.Http.Results.NotFound();

                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                var fileProperty = entity.GetType().GetProperty(_field)!;
                var fileAttribute = fileProperty.GetCustomAttributes(typeof(EntityFileAttribute), false).FirstOrDefault() as EntityFileAttribute;
                var contentTypes = fileAttribute?.ContentTypes;
                
                var entityFile = (EntityFile)fileProperty.GetValue(entity)!;
                var selectedContentType = contentTypes?.FirstOrDefault(c => c.AcceptedExtensions.Any(x => x.Equals(entityFile.Extension, StringComparison.OrdinalIgnoreCase)));
                
                var result = await fileService.DownloadFileAsync(entityFile, _path, cancellationToken);
                
                if (result.IsFailure)
                    return Microsoft.AspNetCore.Http.Results.NotFound();

                return Microsoft.AspNetCore.Http.Results.File(result.Value, selectedContentType?.Name);
            }
        };
}