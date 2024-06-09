using NTextCat;
using PostTranslator_bot;
using RestSharp;

public class Translator
{
    private readonly RankedLanguageIdentifierFactory _factory;
    private readonly RankedLanguageIdentifier _identifier;
    private readonly string _apiKey;
    private readonly string _apiHost;

    public Translator(string apiKey, string apiHost)
    {
        _factory = new RankedLanguageIdentifierFactory();
        var profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core14.profile.xml"); // Используем относительный путь
        _identifier = _factory.Load(profilePath);
        _apiKey = apiKey;
        _apiHost = apiHost;
    }

    public async Task<string> TranslateText(string text)
    {
        var detectedLanguage = DetectLanguage(text);

        if (detectedLanguage == LanguageCode.Russian)
        {
            return text; // Если текст уже на русском, вернуть его как есть
        }

        var translatedText = await TranslateUsingMicrosoftAPI(text, detectedLanguage, LanguageCode.Russian);
        return translatedText;
    }

    private string DetectLanguage(string text)
    {
        var languages = _identifier.Identify(text);
        var mostCertainLanguage = languages.FirstOrDefault();
        if (mostCertainLanguage == null || mostCertainLanguage.Item1.Iso639_3 == "unknown")
        {
            return LanguageCode.AutoDetect;
        }

        return Iso639_3To1(mostCertainLanguage.Item1.Iso639_3);
    }

    private string Iso639_3To1(string iso639_3)
    {
        var iso639_3To1Map = new Dictionary<string, string>
        {
            { "eng", "en" },
            { "rus", "ru" },
            { "spa", "es" },
            { "fra", "fr" },
            { "deu", "de" },
            { "ita", "it" },
        };

        return iso639_3To1Map.ContainsKey(iso639_3) ? iso639_3To1Map[iso639_3] : iso639_3;
    }

    private async Task<string> TranslateUsingMicrosoftAPI(string text, string fromLanguage, string toLanguage)
    {
        var client = new RestClient("https://microsoft-translator-text.p.rapidapi.com");
        var request = new RestRequest("/translate", Method.Post);
        request.AddHeader("X-RapidAPI-Key", _apiKey);
        request.AddHeader("X-RapidAPI-Host", _apiHost);
        request.AddHeader("Content-Type", "application/json");

        // Согласно документации, добавьте параметры запроса
        request.AddQueryParameter("to", toLanguage);
        request.AddQueryParameter("api-version", "3.0");

        // Тело запроса должно быть в формате JSON, содержащем текст для перевода
        var requestBody = new List<Dictionary<string, string>> {
            new Dictionary<string, string> { { "Text", text } }
        };

        request.AddJsonBody(requestBody);

        var response = await client.ExecuteAsync(request);
        if (response.IsSuccessful)
        {
            dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
            return jsonResponse[0].translations[0].text;
        }
        else
        {
            throw new Exception("Translation API request failed: " + response.Content);
        }
    }
}
