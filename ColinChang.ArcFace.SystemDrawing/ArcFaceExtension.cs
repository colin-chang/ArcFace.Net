using System;
using ColinChang.ArcFace.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ColinChang.ArcFace.SystemDrawing
{
    public static class ArcFaceExtension
    {
        public static IServiceCollection AddArcFace(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ArcFaceOptions>()
                .Configure(configuration.Bind)
                .ValidateDataAnnotations();

            services.AddSingleton<IArcFace, ArcFace>();
            services.AddSingleton<IImageProcessor, SystemDrawingProcessor>();
            return services;
        }

        public static IServiceCollection AddArcFace(this IServiceCollection services,
            Action<ArcFaceOptions> configureOptions)
        {
            services.AddOptions<ArcFaceOptions>()
                .Configure(configureOptions)
                .ValidateDataAnnotations();

            services.AddSingleton<IArcFace, ArcFace>();
            services.AddSingleton<IImageProcessor, SystemDrawingProcessor>();
            return services;
        }
    }
}