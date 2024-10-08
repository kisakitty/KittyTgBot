using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KittyBot.dto.gemini;
using KittyBot.exceptions;
using KittyBot.handlers;

namespace KittyBot.services;

public class GeminiBot
{
    private readonly GeminiConfig _generationConfig = new (0.9f, 2000);
    
    private readonly List<GeminiSafetyParameter> _safetyParameter = new()
    {
        new GeminiSafetyParameter("HARM_CATEGORY_HARASSMENT", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_HATE_SPEECH", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_SEXUALLY_EXPLICIT", "BLOCK_NONE"),
        new GeminiSafetyParameter("HARM_CATEGORY_DANGEROUS_CONTENT", "BLOCK_NONE")
    };
    
    private static readonly HttpClient BotClient = new()
    {
        BaseAddress = new Uri("https://generativelanguage.googleapis.com"),
        Timeout = new TimeSpan(0, 1, 0)
    };

    private readonly string? _apikey = Environment.GetEnvironmentVariable(GeminiHandler.GeminiApiKeyEnv);

    public async Task<string> GenerateTextResponse(List<GeminiMessage> contents, string model, CancellationToken cancelToken)
    {
        if (_apikey is null)
        {
            throw new EnvVariablesException($"Expect Gemini API key. Set it to environment variable {GeminiHandler.GeminiApiKeyEnv}");
        }
        var chatRequest = new GeminiRequest(contents, null, _generationConfig, _safetyParameter);
        var httpResponse =
            await BotClient.PostAsJsonAsync($"/v1beta/models/{model}:generateContent?key={_apikey}", chatRequest, cancellationToken: cancelToken);
        string responseText = await httpResponse.Content.ReadAsStringAsync(cancelToken);
        if (httpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new GeminiException(responseText);
        }
        var response = JsonSerializer.Deserialize<GeminiResponse>(responseText);
        var result = response?.candidates?[0].content.parts[0].text;
        if (result is null)
        {
            throw new GeminiException(responseText);
        }
        return result;
    }
}