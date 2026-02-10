#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Afrowave.AJIS.Net;

/// <summary>
/// ASP.NET Core input formatter for AJIS content.
/// </summary>
public class AjisInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextInputFormatter
{
    public AjisInputFormatter()
    {
        SupportedMediaTypes.Add("application/ajis+json");
        SupportedEncodings.Add(System.Text.Encoding.UTF8);
    }

    protected override bool CanReadType(Type type)
    {
        return true; // Support all types
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context, Encoding encoding)
    {
        var httpContext = context.HttpContext;
        var serviceProvider = httpContext.RequestServices;

        using var reader = new StreamReader(httpContext.Request.Body, encoding);
        var content = await reader.ReadToEndAsync();

        try
        {
            // Try to get converter from DI
            var converterType = typeof(AjisConverter<>).MakeGenericType(context.ModelType);
            var converter = serviceProvider.GetService(converterType);

            if(converter == null)
            {
                // Fallback to creating new converter
                var converterFactory = serviceProvider.GetRequiredService<AjisConverterFactory>();
                var getConverterMethod = converterFactory.GetType().GetMethod("GetConverter")!
                    .MakeGenericMethod(context.ModelType);
                converter = getConverterMethod.Invoke(converterFactory, null);
            }

            // Deserialize
            var deserializeMethod = converter.GetType().GetMethod("Deserialize")!;
            var result = deserializeMethod.Invoke(converter, new[] { content });

            return await InputFormatterResult.SuccessAsync(result);
        }
        catch(Exception ex)
        {
            return await InputFormatterResult.FailureAsync();
        }
    }
}

/// <summary>
/// ASP.NET Core output formatter for AJIS content.
/// </summary>
public class AjisOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter
{
    public AjisOutputFormatter()
    {
        SupportedMediaTypes.Add("application/ajis+json");
        SupportedEncodings.Add(System.Text.Encoding.UTF8);
    }

    protected override bool CanWriteType(Type? type)
    {
        return true; // Support all types
    }

    public override async Task WriteResponseBodyAsync(
        OutputFormatterWriteContext context, Encoding encoding)
    {
        var httpContext = context.HttpContext;
        var serviceProvider = httpContext.RequestServices;

        try
        {
            // Get converter from DI
            var converterFactory = serviceProvider.GetRequiredService<AjisConverterFactory>();
            var getConverterMethod = converterFactory.GetType().GetMethod("GetConverter")!
                .MakeGenericMethod(context.ObjectType!);
            var converter = getConverterMethod.Invoke(converterFactory, null);

            // Serialize
            var serializeMethod = converter.GetType().GetMethod("Serialize")!;
            var result = (string)serializeMethod.Invoke(converter, new[] { context.Object })!;

            await httpContext.Response.WriteAsync(result, encoding);
        }
        catch(Exception ex)
        {
            // Fallback to JSON
            await httpContext.Response.WriteAsJsonAsync(context.Object);
        }
    }
}

/// <summary>
/// Extension methods for configuring AJIS in ASP.NET Core.
/// </summary>
public static class AjisAspNetCoreExtensions
{
    /// <summary>
    /// Adds AJIS formatters to MVC.
    /// </summary>
    public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddAjisFormatters(
        this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder)
    {
        builder.AddMvcOptions(options =>
        {
            options.InputFormatters.Add(new AjisInputFormatter());
            options.OutputFormatters.Add(new AjisOutputFormatter());
        });

        builder.Services.AddSingleton<AjisConverterFactory>();
        return builder;
    }

    /// <summary>
    /// Registers an AJIS converter for dependency injection.
    /// </summary>
    public static IServiceCollection AddAjisConverter<T>(
        this IServiceCollection services, AjisSettings? settings = null) where T : notnull
    {
        if(settings != null)
            services.AddSingleton(new AjisConverter<T>(settings));
        else
            services.AddSingleton<AjisConverter<T>>();

        return services;
    }
}