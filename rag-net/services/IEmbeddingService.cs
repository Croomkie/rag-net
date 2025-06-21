using rag_net.Db.Dto;

namespace rag_net.services;

public interface IEmbeddingService
{
    float[] EmbeddingSentence(string sentence);
    Task SaveAllEmbeddingsAsync(IList<CreateEmbeddingChunkDto> chunks);
}