using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
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
            ProductName = chunk.ProductName,
            ChunkIndex = chunk.ChunkIndex
        });

        await context.SaveChangesAsync();
    }
    
    public async Task AddManyAsync(IEnumerable<CreateEmbeddingChunkDto> chunks)
    {
        var entities = chunks.Select(chunk => new EmbeddingChunk
        {
            FileId = chunk.FileId,
            FileName = chunk.FileName,
            FileType = chunk.FileType,
            Page = chunk.Page,
            Url = chunk.Url,
            Chunk = chunk.Chunk,
            Embedding = chunk.Embedding,
            ProductName = chunk.ProductName,
            ChunkIndex = chunk.ChunkIndex
        }).ToList();

        context.EmbeddingChunks.AddRange(entities);
        await context.SaveChangesAsync();
    }

    public async Task<List<GetEmbeddingChunkDto>> SearchByEmbeddingAsync(Vector queryVector, int topK = 5,
        string productName = "rag-net")
    {
        return await context.EmbeddingChunks
            .Where(e => e.ProductName == productName)
            .OrderBy(e => e.Embedding.CosineDistance(queryVector))
            .Take(topK)
            .Select((x) => new GetEmbeddingChunkDto
            {
                FileId = x.FileId,
                FileName = x.FileName,
                FileType = x.FileType,
                Page = x.Page,
                Url = x.Url,
                Chunk = x.Chunk,
                ProductName = x.ProductName,
                ChunkIndex = x.ChunkIndex
            })
            .ToListAsync();
    }
}