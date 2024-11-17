using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace ScaryCavesWeb.Services.Authentication;

public interface IReCaptchaService
{
    Task<bool> IsTokenValid(string token);
}

public class ReCaptchaService(ILogger<ReCaptchaService> logger, ScaryCaveSettings settings, HttpClient httpClient) : IReCaptchaService
{
    private ILogger<ReCaptchaService> Logger { get; } = logger;
    private ScaryCaveSettings Settings { get; } = settings;
    private HttpClient HttpClient { get; } = httpClient;

    public async Task<bool> IsTokenValid(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.google.com/recaptcha/api/siteverify")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = Settings.ReCaptchaSecretKey,
                ["response"] = token
            })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("NotSuccessful response from ReCAPTCHA: {StatusCode}", response.StatusCode);
            return false;
        }

        var jsonResponse = "";
        try
        {
            jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ReCaptchaResponse>(jsonResponse);
            if (result == null)
            {
                Logger.LogError("ReCaptcha Result was null");
                return false;
            }

            if (!result.Success)
            {
                Logger.LogError("Failed to validate ReCaptcha: {@ErrorCodes}", result.ErrorCodes );
                return false;
            }

            if (result.Score < Settings.ReCaptchaScoreThreshold)
            {
                Logger.LogWarning("ReCaptcha Score below threshold: {Score} Errors: {@ErrorCodes}", result.Score, result.ErrorCodes);
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to parse ReCAPTCHA response: \"{Response}\"", jsonResponse);
            return false;
        }
    }
}

public class ReCaptchaResponse
{
    public bool Success { get; set; }
    public float Score { get; set; }
    [JsonProperty("error-codes")] public object?[] ErrorCodes { get; set; } = [];
}
