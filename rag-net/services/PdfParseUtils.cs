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

                foreach (var block in blocks)
                {
                    var cleanBlock = Regex.Replace(block, @"\s+", " ").Trim();
                    if (cleanBlock.Length < 20) continue;

                    List<string> smartChunks;

                    if (cleanBlock.Length <= chunkSize)
                    {
                        smartChunks = new List<string> { cleanBlock };
                    }
                    else if (IsProblematicChunk(cleanBlock))
                    {
                        smartChunks = await openAiChunkService.SmartChunkWithOpenAiAsync(cleanBlock);
                    }
                    else
                    {
                        smartChunks = SmartSentenceChunks(cleanBlock, chunkSize, overlapSize);
                    }

                    foreach (var chunkText in smartChunks)
                    {
                        var finalChunk = chunkText?.Trim();
                        if (string.IsNullOrWhiteSpace(finalChunk) || finalChunk.Length < 10) continue;

                        var embedding = embeddingService.EmbeddingSentence(finalChunk);

                        var dto = new CreateEmbeddingChunkDto
                        {
                            FileId = fileId,
                            FileName = fileName,
                            FileType = fileType,
                            Url = url,
                            Chunk = finalChunk,
                            Page = page.Number,
                            Embedding = new Vector(embedding),
                            ProductName = productName,
                            ChunkIndex = chunkIndex++
                        };

                        chunks.Add(dto);
                    }
                }
            }
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
        // Trop long ou pas assez structuré (peu d'espaces)
        if (text.Length > 800) return true;

        // Manque d'espace après les points ou les virgules
        if (Regex.Matches(text, @"[.,!?][^\s]").Count > 3) return true;

        // Trop dense (trop peu d'espaces par rapport aux caractères)
        if (text.Length > 200 && text.Count(char.IsWhiteSpace) < text.Length / 15) return true;

        return false;
    }
}