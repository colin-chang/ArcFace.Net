using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ColinChang.FaceRecognition
{
    public static class FaceRecognizerExtension
    {
        public static IServiceCollection AddFaceRecognizer(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<FaceRecognitionOptions>()
                .Configure(config.Bind)
                .ValidateDataAnnotations();

            services.AddSingleton<IFaceRecognizer, FaceRecognizer>();
            return services;
        }
    }
}