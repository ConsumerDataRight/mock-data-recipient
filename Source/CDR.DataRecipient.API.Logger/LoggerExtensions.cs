using Microsoft.Extensions.DependencyInjection;

namespace CDR.DataRecipient.API.Logger
{
    /// <summary>
    /// Provides extension methods for registering request and response logging services.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Adds the <see cref="IRequestResponseLogger"/> service to the specified <see cref="IServiceCollection"/> as a singleton.
        /// </summary>
        /// <param name="services">The service collection to add the logger to.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddRequestResponseLogging(this IServiceCollection services)
        {
            services.AddSingleton<IRequestResponseLogger, RequestResponseLogger>();
            return services;
        }
    }
}
