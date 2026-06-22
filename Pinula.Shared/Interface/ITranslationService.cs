using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.Interface
{
    public interface ITranslationService
    {

        public Task<string?> TranslateTextAsync(string text, string targetLanguage);
    }
}
