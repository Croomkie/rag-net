using System.Text.RegularExpressions;
using rag_net.Db.Dto;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace rag_net.services;

public class PdfParseUtils(IEmbeddingService embeddingService) : IPdfParseUtils
{
    public IList<CreateEmbeddingChunkDto> ExtractChunksFromPdf(IFormFileCollection files, int chunkSize = 300)
    {
        IList<CreateEmbeddingChunkDto> chunks = new List<CreateEmbeddingChunkDto>();

        var optimizedChunks = new List<string>();

        foreach (var file in files)
        {
            string fileId = Guid.NewGuid().ToString();
            using var fileStream = file.OpenReadStream();
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            {
                fileStream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            using PdfDocument document = PdfDocument.Open(bytes);

            foreach (Page page in document.GetPages())
            {
                string pageText = Regex.Replace(page.Text, @"\s+", " ");

                // Suppression phrases inutiles récurrentes
                pageText = Regex.Replace(pageText, @"(Malgré.*?constructibilité.*?différentes\.)", "",
                    RegexOptions.IgnoreCase);

                // Découpe en paragraphes via détection intelligente
                var paragraphs = Regex.Split(pageText, @"(?<=\.)\s+|\•|\–|\- ");

                foreach (var paragraph in paragraphs)
                {
                    var cleanParagraph = paragraph.Trim();
                    if (cleanParagraph.Length < 20) continue; // Ignore trop courts ou vides

                    // Chunking intelligent
                    if (cleanParagraph.Length <= chunkSize)
                    {
                        var embedding = embeddingService.EmbeddingSentence(cleanParagraph);

                        var chunk = new CreateEmbeddingChunkDto
                        {
                            FileId = fileId,
                            FileName = file.FileName,
                            FileType = file.ContentType,
                            Url = "url",
                            Chunk = cleanParagraph,
                            Page = page.Number,
                            Embedding = embedding
                        };
                        chunks.Add(chunk);
                    }
                    else
                    {
                        // Découpe en plusieurs chunks de taille idéale (300 char environ)
                        for (int i = 0; i < cleanParagraph.Length; i += chunkSize)
                        {
                            int length = Math.Min(chunkSize, cleanParagraph.Length - i);
                            var embedding =
                                embeddingService.EmbeddingSentence(cleanParagraph.Substring(i, length).Trim());
                            
                            var subChunk = new CreateEmbeddingChunkDto
                            {
                                FileId = fileId,
                                FileName = file.FileName,
                                FileType = file.ContentType,
                                Url = "url",
                                Chunk = cleanParagraph.Substring(i, length).Trim(),
                                Page = page.Number,
                                Embedding = embedding
                            };
                            chunks.Add(subChunk);
                        }
                    }
                }
            }
        }

        return chunks;
    }
}