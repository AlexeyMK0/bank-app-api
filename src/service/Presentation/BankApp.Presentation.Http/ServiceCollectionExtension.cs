namespace Lab1.Presentation.Http;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPresentationHttp(this IServiceCollection collection)
    {
        collection.AddControllers();
        return collection;
    }
}