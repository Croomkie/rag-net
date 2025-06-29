using rag_net.Db.Dto;

namespace rag_net.services;

public interface IOpenAiChunkService
{
    Task<List<string>> SmartChunkWithOpenAiAsync(string content);
    Task<List<GetEmbeddingChunkDto>> RankedChunksOpenAiAsync(string query, List<GetEmbeddingChunkDto> chunks);
    IAsyncEnumerable<string> CompletionAsync(string query, List<GetEmbeddingChunkDto> chunks);
}