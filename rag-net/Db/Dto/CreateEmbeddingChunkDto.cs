using System.ComponentModel.DataAnnotations;
using Pgvector;

namespace rag_net.Db.Dto;

public class CreateEmbeddingChunkDto
{
    public required string FileId { get; init; }

    public required string FileName { get; init; }

    public required string FileType { get; init; }

    [MaxLength(500)]
    public required string Url { get; init; }

    public required string Chunk { get; init; }

    public int Page { get; init; }

    public required Vector Embedding { get; init; }
}