using rag_net.Db.Dto;

namespace rag_net.Repository;

public interface IEmbeddingRepository
{
    Task AddAsync(CreateEmbeddingChunkDto chunk);
}