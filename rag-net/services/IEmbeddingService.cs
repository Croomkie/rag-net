namespace rag_net.services;

public interface IEmbeddingService
{
    float[] EmbeddingSentence(string sentence);
}