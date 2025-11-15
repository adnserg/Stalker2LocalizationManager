using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net;

namespace Stalker2LocalizationManager
{
    /// <summary>
    /// Alternative free translation service using MyMemory Translation API
    /// </summary>
    public class MyMemoryTranslateService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://api.mymemory.translated.net/get";

        public MyMemoryTranslateService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
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
                // MyMemory has a limit of 500 characters per request
                if (text.Length > 500)
                {
                    // Split long text and translate in parts
                    return await TranslateLongText(text, sourceLanguage, targetLanguage);
                }

                return await TranslateChunkAsync(text, sourceLanguage, targetLanguage);
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

        private async Task<string> TranslateLongText(string text, string sourceLanguage, string targetLanguage)
        {
            // Split text into chunks of max 500 characters
            var chunks = new System.Collections.Generic.List<string>();
            var words = text.Split(' ');
            var currentChunk = "";

            foreach (var word in words)
            {
                var testChunk = string.IsNullOrEmpty(currentChunk) ? word : currentChunk + " " + word;
                if (testChunk.Length > 500)
                {
                    if (!string.IsNullOrEmpty(currentChunk))
                    {
                        chunks.Add(currentChunk);
                    }
                    currentChunk = word;
                }
                else
                {
                    currentChunk = testChunk;
                }
            }

            if (!string.IsNullOrEmpty(currentChunk))
            {
                chunks.Add(currentChunk);
            }

            // Translate each chunk
            var translatedParts = new System.Collections.Generic.List<string>();
            foreach (var chunk in chunks)
            {
                var translated = await TranslateChunkAsync(chunk, sourceLanguage, targetLanguage);
                translatedParts.Add(translated);
            }

            return string.Join(" ", translatedParts);
        }

        private async Task<string> TranslateChunkAsync(string text, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var sourceLang = MapLanguageCode(sourceLanguage);
            var targetLang = MapLanguageCode(targetLanguage);

            var url = $"{ApiUrl}?q={WebUtility.UrlEncode(text)}&langpair={sourceLang}|{targetLang}";

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API returned error: {response.StatusCode} - {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<MyMemoryResponse>();
            
            if (result?.ResponseData?.TranslatedText != null && !string.IsNullOrEmpty(result.ResponseData.TranslatedText))
            {
                return result.ResponseData.TranslatedText;
            }

            return text; // Return original if translation fails
        }

        private string MapLanguageCode(string code)
        {
            return code.ToLower() switch
            {
                "zh" => "zh-CN",
                "ja" => "ja",
                "ko" => "ko",
                _ => code.ToLower()
            };
        }

        private class MyMemoryResponse
        {
            public MyMemoryResponseData? ResponseData { get; set; }
        }

        private class MyMemoryResponseData
        {
            public string? TranslatedText { get; set; }
        }
    }
}

