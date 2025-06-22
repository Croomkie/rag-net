using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace rag_net.services;

public class BlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(IOptions<OpenAISettings> options)
    {
        var blobUrl = options.Value.BlobUrl;
        if (string.IsNullOrWhiteSpace(blobUrl))
            throw new InvalidOperationException("URL de stockage manquante !");

        _blobServiceClient = new(
            new Uri(blobUrl),
            new DefaultAzureCredential());
    }

    private BlobContainerClient GetContainerClient(string containerName)
    {
        return _blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> SaveBlobAsync(IFormFile file, string productName)
    {
        try
        {
            var containerClient = GetContainerClient("ragnetpdf");
            bool exists = await containerClient.ExistsAsync();
            if (!exists)
                throw new InvalidOperationException("Le conteneur de stockage n'existe pas !");

            var blobClient = containerClient.GetBlobClient($"{productName}/{file.FileName}");

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            return blobClient.Uri.ToString();
        }
        catch (Exception e)
        {
            throw new Exception("Erreur lors de l'enregistrement du fichier.", e);
        }
    }
}