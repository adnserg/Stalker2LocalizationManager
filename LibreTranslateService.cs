using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Stalker2LocalizationManager
{
    public class LibreTranslateService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public LibreTranslateService(string? apiUrl = null)
        {
            _apiUrl = apiUrl ?? "https://libretranslate.com";
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
                // Map language codes to LibreTranslate format
                var sourceLang = MapLanguageCode(sourceLanguage);
                var targetLang = MapLanguageCode(targetLanguage);

                var url = $"{_apiUrl}/translate";
                var requestBody = new
                {
                    q = text,
                    source = sourceLang,
                    target = targetLang,
                    format = "text"
                };

                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API returned error: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>();
                
                if (result?.TranslatedText != null && !string.IsNullOrEmpty(result.TranslatedText))
                {
                    return result.TranslatedText;
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

        private string MapLanguageCode(string code)
        {
            // LibreTranslate uses ISO 639-1 codes, but some mappings might be needed
            return code.ToLower() switch
            {
                "zh" => "zh",
                "ja" => "ja",
                "ko" => "ko",
                _ => code.ToLower()
            };
        }

        private class LibreTranslateResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("translatedText")]
            public string? TranslatedText { get; set; }
        }
    }
}

