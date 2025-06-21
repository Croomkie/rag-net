namespace rag_net.services;

public interface IPdfParseUtils
{
    IList<string> ExtractChunksFromPdf(IFormFileCollection files, int chunkSize = 300);
}