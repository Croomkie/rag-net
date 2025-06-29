using rag_net.Db;
using rag_net.Db.Dto;

namespace rag_net.services;

public interface IEmbeddingService
{
    Task<float[]> EmbeddingSentence(string sentence);
    Task<List<float[]>> EmbeddingSentences(List<string> chunks);
    Task SaveAllEmbeddingsAsync(IList<CreateEmbeddingChunkDto> chunks);
    Task<List<GetEmbeddingChunkDto>> SearchByEmbeddingAsync(string query, string productName = "rag-net");
    IAsyncEnumerable<string> ChatResponseAsync(string query, string productName = "rag-net");
}