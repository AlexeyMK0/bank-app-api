using System.Text.Json.Serialization;

namespace BankApp.Gateway.Presentation.Http;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPresentationHttp(this IServiceCollection collection)
    {
        collection.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters
                .Add(new JsonStringEnumConverter()));
        return collection;
    }

    public static WebApplication UsePresentationHttp(this WebApplication application)
    {
        application.MapControllers();
        return application;
    }
}