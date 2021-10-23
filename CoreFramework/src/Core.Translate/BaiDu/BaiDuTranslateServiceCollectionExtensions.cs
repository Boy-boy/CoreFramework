using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;

namespace Core.Translate.BaiDu
{
    public static class BaiDuTranslateServiceCollectionExtensions
    {
        /// <summary>
        /// 添加百度翻译
        /// </summary>
        /// <param name="services"></param>
        /// <param name="optionAction"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddBaiDuTranslate(this IServiceCollection services, Action<BaiDuTranslateOptions> optionAction = default)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (optionAction != null)
                services.Configure(optionAction);

            services.TryAddSingleton(typeof(ITranslateProvider), typeof(BaiDuTranslateProvider));

            services.AddHttpClient("BaiDuTranslate")
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                })
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(5)
                }));
            return services;
        }
    }
}
