using rag_net.Db;
using rag_net.Db.Dto;

namespace rag_net.services;

public interface IEmbeddingService
{
    float[] EmbeddingSentence(string sentence);
    Task SaveAllEmbeddingsAsync(IList<CreateEmbeddingChunkDto> chunks);
    Task<List<GetEmbeddingChunkDto>> SearchByEmbeddingAsync(string query, string productName = "rag-net");
}