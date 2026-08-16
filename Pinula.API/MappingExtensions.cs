using Mapster;

namespace Pinula.API;

public static class MappingExtensions
{
    public static TDestination AdaptWithRequest<TDestination>(this object source, HttpRequest request)
    {
        return source.BuildAdapter()
            .AddParameters("httpRequest", request)
            .AdaptToType<TDestination>();
    }
    
}