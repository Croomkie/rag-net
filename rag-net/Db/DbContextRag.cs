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
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is EmbeddingChunk && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            ((EmbeddingChunk)entityEntry.Entity).UpdateAt = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                ((EmbeddingChunk)entityEntry.Entity).CreateAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
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
    
    public int ChunkIndex { get; init; }
    
    public DateTime CreateAt { get; set; }
    
    public DateTime UpdateAt { get; set; }
}