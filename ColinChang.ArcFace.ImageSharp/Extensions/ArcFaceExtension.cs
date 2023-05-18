using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ColinChang.ArcFace.Abstraction;
using ColinChang.ArcFace.Core;

namespace ColinChang.ArcFace.ImageSharp.Extensions
{
    public static class ArcFaceExtension
    {
        public static IServiceCollection AddArcFace(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ArcFaceOptions>()
                .Configure(configuration.Bind)
                .ValidateDataAnnotations();

            services.AddSingleton<IArcFace, Core.ArcFace>();
            services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
            return services;
        }

        public static IServiceCollection AddArcFace(this IServiceCollection services,
            Action<ArcFaceOptions> configureOptions)
        {
            services.AddOptions<ArcFaceOptions>()
                .Configure(configureOptions)
                .ValidateDataAnnotations();

            services.AddSingleton<IArcFace, Core.ArcFace>();
            services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
            return services;
        }
    }
}