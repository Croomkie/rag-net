using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace rag_net.Db;

public class DbContextRag(DbContextOptions<DbContextRag> options) : DbContext(options)
{
    DbSet<EmbeddingChunk> EmbeddingChunk { get; set; }
}

public class EmbeddingChunk
{
    public Guid Id { get; set; }

    public required string FileId { get; init; }

    public required string FileName { get; init; }
    
    public required string FileType { get; init; }

    [MaxLength(500)] public required string Url { get; init; }

    public required string Chunk { get; init; }

    public int Page { get; init; }

    public float[] Embedding { get; init; } = [];

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreateAt { get; init; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdateAt { get; init; }
}