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

    public EmbeddingService(IOptions<OpenAISettings> options, IEmbeddingRepository repository)
    {
        _repository = repository;
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
        foreach (var chunk in chunks)
        {
            await _repository.AddAsync(chunk);
        }
    }

    public async Task<List<GetEmbeddingChunkDto>> SearchByEmbeddingAsync(string query)
    {
        var embeddingFloat = EmbeddingSentence(query);
        return await _repository.SearchByEmbeddingAsync(new Vector(embeddingFloat));
    }
}