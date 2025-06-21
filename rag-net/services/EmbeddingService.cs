using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace rag_net.services;

public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;

    public EmbeddingService(IOptions<OpenAISettings> options)
    {
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
}