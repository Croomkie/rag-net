using Azure.Storage.Blobs;

namespace rag_net.services;

public interface IBlobService
{
    Task<string> SaveBlobAsync(IFormFile file, string productName);
}