using rag_net.Db.Dto;

namespace rag_net.services;

public interface IPdfParseUtils
{
    Task<IList<CreateEmbeddingChunkDto>> ExtractChunksFromPdf(IFormFileCollection files, int chunkSize = 300,
        string productName = "rag-net");
}