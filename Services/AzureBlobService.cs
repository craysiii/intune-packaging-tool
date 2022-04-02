using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using IntunePackagingTool.Models;

namespace IntunePackagingTool.Services;

public class AzureBlobService
{
    private readonly string? _azureBlobUrl;
    
    private IHostApplicationLifetime? ApplicationLifetime { get; }
    private ILogger<AzureBlobService> Logger { get; }

    public AzureBlobService(ILogger<AzureBlobService> logger ,IHostApplicationLifetime applicationLifetime)
    {
        Logger = logger;
        ApplicationLifetime = applicationLifetime;
        
        var blobUrl = Environment.GetEnvironmentVariable("AZURE_BLOB_URL");
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            ApplicationLifetime.StopApplication();
        }
        else
        {
            _azureBlobUrl = blobUrl;
        }
    }

    public async Task UploadBlob(AzureUpload upload)
    {
        try
        {
            var container = new BlobContainerClient(_azureBlobUrl, upload.PackageId);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            BlobClientOptions blobClientOptions = new()
            {
                Transport = new HttpClientTransport(
                    new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
            };

            var blob = new BlockBlobClient(_azureBlobUrl, upload.PackageId, upload.File?.Name, blobClientOptions);
            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

            StorageTransferOptions transferOptions = new()
            {
                InitialTransferSize = 1024 * 1024 * 5,
                MaximumTransferSize = 1024 * 1024 * 5,
                MaximumConcurrency = Environment.ProcessorCount
            };

            BlobUploadOptions blobUploadOptions = new()
            {
                TransferOptions = transferOptions,
                HttpHeaders = new BlobHttpHeaders { ContentType = upload.File?.ContentType },
                ProgressHandler = upload.Progress
            };

            // 5GB max file size
            const long maxFileSize = 5368709120;
            await using var fileStream = upload.File?.OpenReadStream(maxFileSize);
            await blob.UploadAsync(fileStream, blobUploadOptions, upload.CancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError("Error Uploading Blob {}/{}: {}",upload.PackageId, upload.File?.Name, e.Message);
        }
    }

    public async Task DeleteBlob(string containerName, string blobName)
    {
        try
        {
            var container = new BlobContainerClient(_azureBlobUrl, containerName);
            await container.DeleteBlobAsync(blobName);
        }
        catch (Exception e)
        {
            Logger.LogError("Error Deleting Blob {}/{}: {}", containerName, blobName, e.Message);
        }
    }

    public async Task DeleteContainer(string containerName)
    {
        try
        {
            var container = new BlobContainerClient(_azureBlobUrl, containerName);
            await container.DeleteAsync();
        }
        catch (Exception e)
        {
            Logger.LogError("Error Deleting Container {}: ", containerName);
        }
    }

    public async Task DeleteContainers()
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_azureBlobUrl);
            var containers = blobServiceClient.GetBlobContainers();

            foreach (var container in containers)
            {
                await DeleteContainer(container.Name);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Error Deleting Containers: {}", e.Message);
        }
        
    }
}