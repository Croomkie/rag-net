using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Pgvector;
using rag_net.Db;
using rag_net.Db.Dto;
using rag_net.Repository;

namespace rag_net.services;

public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly IEmbeddingRepository _repository;
    private readonly IOpenAiChunkService _openAiChunkService;

    public EmbeddingService(IOptions<OpenAISettings> options, IEmbeddingRepository repository,
        IOpenAiChunkService openAiChunkService)
    {
        _repository = repository;
        _openAiChunkService = openAiChunkService;
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Clé API OpenAI manquante !");
        _client = new EmbeddingClient("text-embedding-3-small", apiKey);
    }

    public float[] EmbeddingSentence(string sentence)
    {
        try
        {
            OpenAIEmbedding embedding = _client.GenerateEmbedding(sentence);

            return embedding.ToFloats().ToArray();
        }
        catch (Exception e)
        {
            throw new Exception("Erreur lors de la génération de l'embedding.", e);
        }
    }

    public async Task SaveAllEmbeddingsAsync(IList<CreateEmbeddingChunkDto> chunks)
    {
        await _repository.AddManyAsync(chunks);
    }

    public async Task<List<GetEmbeddingChunkDto>> SearchByEmbeddingAsync(string query, string productName = "rag-net")
    {
        var embeddingFloat = EmbeddingSentence(query);
        var chunks = await _repository.SearchByEmbeddingAsync(new Vector(embeddingFloat), 5, productName);

        return await _openAiChunkService.RankedChunksOpenAiAsync(query, chunks);
    }
}