using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KittyBot.dto.gemini;
using KittyBot.exceptions;
using KittyBot.handlers;

namespace KittyBot.services;

public class GeminiBot
{
    private static readonly HttpClient BotClient = new()
    {
        BaseAddress = new Uri("https://generativelanguage.googleapis.com"),
        Timeout = new TimeSpan(0, 1, 0)
    };

    private readonly string? _apikey = Environment.GetEnvironmentVariable(GeminiHandler.GeminiApiKeyEnv);

    private readonly GeminiConfig _generationConfig = new(0.9f, 2000);
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    // https://ai.google.dev/gemini-api/docs/safety-settings
    private readonly List<GeminiSafetyParameter> _safetyParameter =
    [
        new GeminiSafetyParameter("HARM_CATEGORY_HARASSMENT", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_HATE_SPEECH", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_SEXUALLY_EXPLICIT", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_DANGEROUS_CONTENT", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_CIVIC_INTEGRITY", "BLOCK_NONE")
    ];

    public async Task<string> GenerateTextResponse(List<GeminiMessage> contents, string model,
        CancellationToken cancelToken, string? systemInstruction = null)
    {
        if (_apikey is null)
            throw new EnvVariablesException(
                $"Expect Gemini API key. Set it to environment variable {GeminiHandler.GeminiApiKeyEnv}");

        var geminiSystemInstruction = systemInstruction != null
            ? new GeminiSystemInstruction(new GeminiText(systemInstruction))
            : null;
        var chatRequest = new GeminiRequest(contents, geminiSystemInstruction, _generationConfig, _safetyParameter);
        var httpResponse =
            await BotClient.PostAsJsonAsync($"/v1beta/models/{model}:generateContent?key={_apikey}", chatRequest,
                cancelToken);
        var responseText = await httpResponse.Content.ReadAsStringAsync(cancelToken);
        if (httpResponse.StatusCode != HttpStatusCode.OK) throw new GeminiException(responseText);

        var response = JsonSerializer.Deserialize<GeminiResponse>(responseText, _jsonSerializerOptions);
        var candidates = response?.candidates;
        if (candidates == null || candidates.Count == 0) throw new GeminiException(responseText);
        var result = candidates[0]?.content?.parts[0].text;
        if (result == null) throw new GeminiException(responseText);
        return result;
    }

    public List<GeminiMessage> BuildMessage(string content, string? role)
    {
        return [new GeminiMessage([new GeminiContent(content, null)], role)];
    }
}