using System.Globalization;
using Mapster;

namespace Pinula.API;

public static class MappingExtensions
{
    //Obsolete - Do not use, use AdaptWithLangAndBaseUrl instead
    public static TDestination AdaptWithRequest<TDestination>(this object source, HttpRequest request)
    {
        string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        string baseUrl = $"{request.Scheme}://{request.Host}";

        return source.AdaptWithLangAndBaseUrl<TDestination>(baseUrl, lang);
    }
    
    public static TDestination AdaptWithLangAndBaseUrl<TDestination>(this object source, string baseUrl, string lang)
    {
        return source.BuildAdapter()
            .AddParameters("baseUrl", baseUrl)
            .AddParameters("lang", lang)
            .AdaptToType<TDestination>();
    }
    
}