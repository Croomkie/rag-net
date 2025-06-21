using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace rag_net.services;

public class PdfParseUtils : IPdfParseUtils
{
    public IList<string> ExtractChunksFromPdf(IFormFileCollection files, int chunkSize = 300)
    {
        using var fileStream = files[0].OpenReadStream();
        byte[] bytes;

        using (var memoryStream = new MemoryStream())
        {
            fileStream.CopyTo(memoryStream);
            bytes = memoryStream.ToArray();
        }

        var optimizedChunks = new List<string>();

        using (PdfDocument document = PdfDocument.Open(bytes))
        {
            foreach (Page page in document.GetPages())
            {
                string pageText = Regex.Replace(page.Text, @"\s+", " ");

                // Suppression phrases inutiles récurrentes
                pageText = Regex.Replace(pageText, @"(Malgré.*?constructibilité.*?différentes\.)", "", RegexOptions.IgnoreCase);

                // Découpe en paragraphes via détection intelligente
                var paragraphs = Regex.Split(pageText, @"(?<=\.)\s+|\•|\–|\- ");

                foreach (var paragraph in paragraphs)
                {
                    var cleanParagraph = paragraph.Trim();
                    if (cleanParagraph.Length < 20) continue; // Ignore trop courts ou vides

                    // Chunking intelligent
                    if (cleanParagraph.Length <= chunkSize)
                    {
                        optimizedChunks.Add(cleanParagraph);
                    }
                    else
                    {
                        // Découpe en plusieurs chunks de taille idéale (300 char environ)
                        for (int i = 0; i < cleanParagraph.Length; i += chunkSize)
                        {
                            int length = Math.Min(chunkSize, cleanParagraph.Length - i);
                            optimizedChunks.Add(cleanParagraph.Substring(i, length).Trim());
                        }
                    }
                }
            }
        }

        return optimizedChunks;
    }
}