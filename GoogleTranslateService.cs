using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Stalker2LocalizationManager
{
    public class GoogleTranslateService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string TranslateApiUrl = "https://translation.googleapis.com/language/translate/v2";

        public GoogleTranslateService(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testText = "Hello";
                var translated = await TranslateAsync(testText, "en", "ru");
                return !string.IsNullOrEmpty(translated);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            try
            {
                var url = $"{TranslateApiUrl}?key={_apiKey}";
                var requestBody = new
                {
                    q = text,
                    source = sourceLanguage,
                    target = targetLanguage,
                    format = "text"
                };

                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API returned error: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<TranslateResponse>();
                
                if (result?.Data?.Translations != null && result.Data.Translations.Count > 0)
                {
                    var translatedText = result.Data.Translations[0].TranslatedText;
                    if (string.IsNullOrEmpty(translatedText))
                    {
                        throw new Exception("Translated text is empty");
                    }
                    return translatedText;
                }

                throw new Exception("Translation response is empty");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP error during translation: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Request timeout: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during translation: {ex.Message}", ex);
            }
        }

        private class TranslateResponse
        {
            public TranslateData? Data { get; set; }
        }

        private class TranslateData
        {
            public System.Collections.Generic.List<Translation>? Translations { get; set; }
        }

        private class Translation
        {
            public string? TranslatedText { get; set; }
        }
    }
}

