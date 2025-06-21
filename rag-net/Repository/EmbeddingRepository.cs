using rag_net.Db;
using rag_net.Db.Dto;

namespace rag_net.Repository;

public class EmbeddingRepository(DbContextRag context) : IEmbeddingRepository
{
    public async Task AddAsync(CreateEmbeddingChunkDto chunk)
    {
        context.EmbeddingChunks.Add(new EmbeddingChunk
        {
            FileId = chunk.FileId,
            FileName = chunk.FileName,
            FileType = chunk.FileType,
            Page = chunk.Page,
            Url = chunk.Url,
            Chunk = chunk.Chunk,
            Embedding = chunk.Embedding,
        });

        await context.SaveChangesAsync();
    }
    
    
}