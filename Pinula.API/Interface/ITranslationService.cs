using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.API.Interface
{
    public interface ITranslationService
    {
        public Task<string?> TranslateTextAsync(string text, string targetLanguage);
    }
}
