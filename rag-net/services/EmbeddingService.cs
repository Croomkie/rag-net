using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Pgvector;
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

    public async Task<float[]> EmbeddingSentence(string sentence)
    {
        try
        {
            OpenAIEmbedding embedding = await _client.GenerateEmbeddingAsync(sentence);

            return embedding.ToFloats().ToArray();
        }
        catch (Exception e)
        {
            throw new Exception("Erreur lors de la génération de l'embedding.", e);
        }
    }

    public async Task<List<float[]>> EmbeddingSentences(List<string> chunks)
    {
        List<float[]> vectors = new List<float[]>();

        OpenAIEmbeddingCollection collection = await _client.GenerateEmbeddingsAsync(chunks);
        foreach (OpenAIEmbedding embedding in collection)
        {
            float[] vector = embedding.ToFloats().ToArray();

            vectors.Add(vector);
        }

        return vectors;
    }

    public async Task SaveAllEmbeddingsAsync(IList<CreateEmbeddingChunkDto> chunks)
    {
        await _repository.AddManyAsync(chunks);
    }

    public async Task<List<GetEmbeddingChunkDto>> SearchByEmbeddingAsync(string query, string productName = "rag-net")
    {
        return await RankedChunksAsync(query, 5, productName);
    }

    private async Task<List<GetEmbeddingChunkDto>> RankedChunksAsync(string query, int topK, string productName)
    {
        var embeddingFloat = await EmbeddingSentence(query);
        var chunks = await _repository.SearchByEmbeddingAsync(new Vector(embeddingFloat), topK, productName);

        return await _openAiChunkService.RankedChunksOpenAiAsync(query, chunks);
    }

    public async IAsyncEnumerable<string> ChatResponseAsync(string query, string productName = "rag-net")
    {
        var chunks = await RankedChunksAsync(query, 15, productName);

        if (chunks.Count == 0)
        {
            yield return "Aucun résultat trouvé.";
            yield break;
        }


        await foreach (var token in _openAiChunkService.CompletionAsync(query, chunks))
        {
            yield return token;
        }
    }
}