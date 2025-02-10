using System.Text.Json;

namespace CSharpEssentials.RecaptchaHelper;
public class RecaptchaService {
    private readonly HttpClient _httpClient;
    public RecaptchaService(HttpClient httpClient) {
        _httpClient = httpClient;
    }
    /// <summary>
    /// Questa Chiamata viene effettuata dal frontend che recupera il token inviato da Google a sua volta questo token deve essere inviato a Google per ricevere lo score che vale da 0 a 1
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<RecaptchaResponse> VerifyTokenAsync(string token) {
        var secretKey = "<YOUR_SECRET_KEY>"; //TODO: da configurare sul DB
        var response = await _httpClient.PostAsync(
            $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}", //TODO: da configurare sul sito 
            null
        );
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RecaptchaResponse>(json);

        return result;
        //return new(result?.Success == true && result.Score > 0.5, json); //TODO: da configurare sul DB
    }
}

public class RecaptchaResponse {
    public bool success { get; set; }
    public DateTime challenge_ts { get; set; }
    public string hostname { get; set; }
    public float score { get; set; }
    public string action { get; set; }
}
