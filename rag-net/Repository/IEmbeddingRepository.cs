using Pgvector;
using rag_net.Db;
using rag_net.Db.Dto;

namespace rag_net.Repository;

public interface IEmbeddingRepository
{
    Task AddAsync(CreateEmbeddingChunkDto chunk);

    Task<List<GetEmbeddingChunkDto>> SearchByEmbeddingAsync(Vector queryVector, int topK = 5, string productName = "rag-net");
}