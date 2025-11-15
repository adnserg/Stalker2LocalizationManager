using System.Threading.Tasks;

namespace Stalker2LocalizationManager
{
    public interface ITranslationService
    {
        Task<bool> TestConnectionAsync();
        Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    }
}

