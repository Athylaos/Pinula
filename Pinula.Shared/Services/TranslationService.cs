using Pinula.Shared.Interface;
using Microsoft.Extensions.Configuration;
using DeepL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly Translator _translator;

        public TranslationService(IConfiguration config)
        {
            var apiKey = config["DeepL:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "DeepL ApiKey missing in configuration");
            }
            _translator = new Translator(apiKey);
        }
        public async Task<string?> TranslateTextAsync(string text, string targetLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var targetLang = targetLanguageCode.ToLower() == "en" ? LanguageCode.EnglishAmerican : LanguageCode.Czech;

            var result = await _translator.TranslateTextAsync(text, null, targetLang);
            return result.Text;

        }
    }
}
