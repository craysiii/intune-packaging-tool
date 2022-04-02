using IntunePackagingTool.Services;
using Microsoft.AspNetCore.Components.Forms;


namespace IntunePackagingTool.Models;

public class AzureUpload
{
    public IBrowserFile? File { get; }
    public string PackageId { get; }
    public Progress<long>? Progress { get; }
    public int PercentageComplete { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; }
    public CancellationToken CancellationToken { get; }
    private AzureBlobService AzureBlobService { get; }
    
    

    public AzureUpload(IBrowserFile file, string packageId, AzureBlobService azureBlobService)
    {
        this.File = file;
        this.PackageId = packageId;

        Progress = new Progress<long>();
        Progress.ProgressChanged += UploadProgressChanged;

        CancellationTokenSource = new CancellationTokenSource();
        CancellationToken = CancellationTokenSource.Token;

        AzureBlobService = azureBlobService;
    }
    
    public async Task CancelOrDelete()
    {
        if (PercentageComplete == 100 && File is not null)
        {
            await AzureBlobService.DeleteBlob(PackageId, File.Name);
        }
        else
        {
            CancellationTokenSource?.Cancel();
        }
    }

    private void UploadProgressChanged(object? sender, long bytesUploaded)
    {
        if (File is not null)
        {
            PercentageComplete = (int)Math.Floor(((double)bytesUploaded / File.Size) * 100);
        }
    }
}