using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace rag_net.Db;

public class DbContextRag(DbContextOptions<DbContextRag> options) : DbContext(options)
{
    public DbSet<EmbeddingChunk> EmbeddingChunks { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        
        modelBuilder.Entity<EmbeddingChunk>()
            .Property(e => e.Embedding)
            .HasColumnType("vector(1536)");
        
        modelBuilder.Entity<EmbeddingChunk>()
            .Property(e => e.CreateAt)
            .HasDefaultValueSql("now()");
    }
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

    [Column(TypeName = "vector(3)")]
    public Vector Embedding { get; set; }
    
    public string ProductName { get; init; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreateAt { get; init; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdateAt { get; init; }
}