using Asp.Versioning.ApiExplorer;

namespace Framework.Api.Extensions;

/// <summary>
/// Swagger/OpenAPI configuration extensions
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static IServiceCollection AddVersionedSwagger(
        this IServiceCollection services,
        IApiVersionDescriptionProvider provider)
    {
        services.AddSwaggerGen();
        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Framework API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Framework API";
        });

        return app;
    }
}
