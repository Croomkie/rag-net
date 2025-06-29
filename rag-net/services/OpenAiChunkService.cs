using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using rag_net.Db.Dto;

namespace rag_net.services;

public class OpenAiChunkService : IOpenAiChunkService
{
    private readonly ChatClient _client;

    public OpenAiChunkService(IOptions<OpenAISettings> options)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Clé API OpenAI manquante !");

        _client = new ChatClient("gpt-4o-mini", apiKey);
    }

    public async Task<List<string>> SmartChunkWithOpenAiAsync(string content)
    {
        var messages = new List<ChatMessage>
        {
            new UserChatMessage($"""
                                 Découpe précisément le texte ci-dessous en blocs cohérents pour une utilisation optimale dans un système RAG (Recherche augmentée par génération). 
                                 Voici les règles OBLIGATOIRES à suivre :

                                 1. Chaque bloc doit contenir une seule thématique claire et unique (exemple : formation, expérience professionnelle, compétences techniques, centres d'intérêt).
                                 2. Aucun bloc ne doit commencer ou terminer en coupant une phrase, un mot, ou une section importante.
                                 3. Ne change aucun mot, aucune information, ni la ponctuation originale.
                                 4. Retourne uniquement le JSON avec la structure fournie ci-dessous :

                                 Texte à découper:
                                 {content}
                                 """)
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "smart_chunking",
                jsonSchema: BinaryData.FromString("""
                                                  {
                                                      "type": "object",
                                                      "properties": {
                                                          "chunks": {
                                                              "type": "array",
                                                              "items": {
                                                                  "type": "object",
                                                                  "properties": {
                                                                      "chunkText": { "type": "string" }
                                                                  },
                                                                  "required": ["chunkText"],
                                                                  "additionalProperties": false
                                                              }
                                                          }
                                                      },
                                                      "required": ["chunks"],
                                                      "additionalProperties": false
                                                  }
                                                  """),
                jsonSchemaIsStrict: true)
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages, options);

        using JsonDocument structuredJson = JsonDocument.Parse(completion.Content[0].Text);

        var chunks = new List<string>();
        foreach (var chunkElement in structuredJson.RootElement.GetProperty("chunks").EnumerateArray())
        {
            chunks.Add(chunkElement.GetProperty("chunkText").GetString() ?? throw new ArgumentNullException());
        }

        return chunks;
    }

    public async Task<List<GetEmbeddingChunkDto>> RankedChunksOpenAiAsync(string query,
        List<GetEmbeddingChunkDto> chunks)
    {
        List<ChatMessage> messages =
        [
            new UserChatMessage($$"""
                                  Tu es un assistant intelligent chargé de classer des extraits de documents (appelés "chunks") selon leur pertinence par rapport à une question utilisateur.

                                  Voici la question :
                                  "{{query}}"

                                  Et voici les extraits de texte numérotés :

                                  {{FormatChunksForPrompt(chunks)}}

                                  Ta tâche :
                                  - Analyse le contenu de chaque chunk.
                                  - Classe-les **du plus pertinent au moins pertinent** pour répondre à la question.
                                  - Donne un score de pertinence de 0 à 100 pour chaque chunk.
                                  - Retourne uniquement le JSON avec la structure fournie.
                                  - Chaque chunk est précédé de son ID unique, sous la forme “Chunk {GUID}:”
                                  - Tu dois retourner uniquement un JSON contenant une liste “rankedChunks” avec les champs : "id" (le GUID du chunk) et "score" (0 à 100).


                                  Aucune explication, aucun texte supplémentaire. Respecte uniquement le format JSON.
                                  """)
        ];

        ChatCompletionOptions options = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "math_reasoning",
                jsonSchema: BinaryData.FromBytes("""
                                                 {
                                                   "type": "object",
                                                   "properties": {
                                                     "rankedChunks": {
                                                       "type": "array",
                                                       "items": {
                                                         "type": "object",
                                                         "properties": {
                                                           "id": { "type": "string" },
                                                           "score": { "type": "integer", "minimum": 0, "maximum": 100 }
                                                         },
                                                         "required": ["id", "score"],
                                                         "additionalProperties": false
                                                       }
                                                     }
                                                   },
                                                   "required": ["rankedChunks"],
                                                   "additionalProperties": false
                                                 }
                                                 """u8.ToArray()),
                jsonSchemaIsStrict: true)
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages, options);

        using JsonDocument structuredJson = JsonDocument.Parse(completion.Content[0].Text);

        foreach (JsonElement stepElement in structuredJson.RootElement.GetProperty("rankedChunks").EnumerateArray())
        {
            var id = new Guid(stepElement.GetProperty("id").ToString());
            var score = stepElement.GetProperty("score").GetInt32();

            var chunk = chunks.FirstOrDefault(c => c.Id == id);

            if (chunk != null)
                chunk.Score = score;
        }

        return chunks
            .Where(x => x.Score != null)
            .OrderByDescending(x => x.Score)
            .ToList();
    }

    private static string FormatChunksForPrompt(List<GetEmbeddingChunkDto> chunks)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < chunks.Count; i++)
        {
            sb.AppendLine($"Chunk {chunks[i].Id}:");
            sb.AppendLine(chunks[i].Chunk.Replace("\n", " "));
            sb.AppendLine();
        }

        return sb.ToString();
    }
}