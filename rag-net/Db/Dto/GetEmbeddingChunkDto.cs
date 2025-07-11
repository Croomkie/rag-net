﻿using System.ComponentModel.DataAnnotations;

namespace rag_net.Db.Dto;

public class GetEmbeddingChunkDto
{
    public required Guid Id { get; init; }
    public required string FileId { get; init; }

    public required string FileName { get; init; }

    public required string FileType { get; init; }

    [MaxLength(500)]
    public required string Url { get; init; }

    public required string Chunk { get; init; }

    public int Page { get; init; }
    
    public required string ProductName { get; init; }
    
    public int ChunkIndex { get; init; }
    
    public int? Score { get; set; }
}