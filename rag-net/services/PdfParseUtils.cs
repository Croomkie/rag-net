using System.Text.RegularExpressions;
using Pgvector;
using rag_net.Db.Dto;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace rag_net.services;

public class PdfParseUtils(
    IEmbeddingService embeddingService,
    IBlobService blobService,
    IOpenAiChunkService openAiChunkService) : IPdfParseUtils
{
    public async Task<IList<CreateEmbeddingChunkDto>> ExtractChunksFromPdf(
        IFormFileCollection files,
        int chunkSize = 300,
        string productName = "rag-net")
    {
        int overlapSize = 80;

        var chunks = new List<CreateEmbeddingChunkDto>();
        var finalChunkTexts =
            new List<(string Text, int Page, string FileId, string FileName, string FileType, string Url, string
                ProductName, int ChunkIndex)>();

        foreach (var file in files)
        {
            string fileId = Guid.NewGuid().ToString();
            var fileName = file.FileName;
            var fileType = file.ContentType;
            string url = await blobService.SaveBlobAsync(file, productName);

            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                bytes = memoryStream.ToArray();
            }

            using var document = PdfDocument.Open(bytes);
            foreach (var page in document.GetPages())
            {
                string rawText = page.Text.Replace("\r", "").Replace("\t", "");
                var blocks = Regex.Split(rawText, @"\n{2,}|\r\n{2,}");

                int chunkIndex = 0;
                int smartChunkCount = 0;

                foreach (var block in blocks)
                {
                    var cleanBlock = Regex.Replace(block, @"\s+", " ").Trim();
                    if (cleanBlock.Length < 20) continue;

                    List<string> smartChunks;

                    if (cleanBlock.Length <= chunkSize)
                    {
                        smartChunks = new List<string> { cleanBlock };
                    }
                    else if (IsProblematicChunk(cleanBlock) && smartChunkCount < 5)
                    {
                        smartChunks = await openAiChunkService.SmartChunkWithOpenAiAsync(cleanBlock);
                        smartChunkCount++;
                    }
                    else
                    {
                        smartChunks = SmartSentenceChunks(cleanBlock, chunkSize, overlapSize);
                    }

                    foreach (var chunkText in smartChunks)
                    {
                        var finalChunk = chunkText?.Trim();
                        if (string.IsNullOrWhiteSpace(finalChunk) || finalChunk.Length < 10) continue;

                        finalChunkTexts.Add((finalChunk, page.Number, fileId, fileName, fileType, url, productName,
                            chunkIndex++));
                    }
                }
            }
        }

        // 🧠 1 seul appel embedding pour tous les chunks
        var textsOnly = finalChunkTexts.Select(t => t.Text).ToList();
        var embeddings = await embeddingService.EmbeddingSentences(textsOnly);

        // Associe chunks + embeddings
        for (int i = 0; i < finalChunkTexts.Count; i++)
        {
            var meta = finalChunkTexts[i];
            var vector = new Vector(embeddings[i]);

            var dto = new CreateEmbeddingChunkDto
            {
                FileId = meta.FileId,
                FileName = meta.FileName,
                FileType = meta.FileType,
                Url = meta.Url,
                Chunk = meta.Text,
                Page = meta.Page,
                Embedding = vector,
                ProductName = meta.ProductName,
                ChunkIndex = meta.ChunkIndex
            };

            chunks.Add(dto);
        }

        return chunks;
    }

    private static List<string> SmartSentenceChunks(string text, int maxLength = 300, int overlap = 80)
    {
        var chunks = new List<string>();
        var sentences = Regex.Split(text, @"(?<=[\.!\?])\s+");
        var current = "";

        foreach (var sentence in sentences)
        {
            if ((current + sentence).Length > maxLength)
            {
                chunks.Add(current.Trim());

                // Génère overlap (avec les dernières phrases)
                var words = current.Split(' ');
                var overlapWords = words.Skip(Math.Max(0, words.Length - (overlap / 5))).ToArray(); // ~5 chars/word
                current = string.Join(" ", overlapWords) + " " + sentence;
            }
            else
            {
                current += " " + sentence;
            }
        }

        if (!string.IsNullOrWhiteSpace(current))
            chunks.Add(current.Trim());

        return chunks;
    }

    bool IsProblematicChunk(string text)
    {
        // Très long + peu structuré
        if (text.Length > 1000 && text.Count(char.IsWhiteSpace) < text.Length / 10)
            return true;

        return false;
    }
}