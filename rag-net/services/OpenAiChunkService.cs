using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

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
}