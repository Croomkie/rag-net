namespace rag_net.services;

public interface IOpenAiChunkService
{
    Task<List<string>> SmartChunkWithOpenAiAsync(string content);
}