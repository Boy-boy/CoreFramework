using System;
using Core.ElasticSearch.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.ElasticSearch
{
    public static class ElasticSearchServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticClientFactory(this IServiceCollection services)
        {
            services.AddOptions();
            services.TryAddSingleton<IElasticClientFactory, ElasticClientFactory>();
            return services;
        }

        public static DefaultElasticClientBuilder AddElasticClientFactory(this IServiceCollection services,
            string name,
            Action<ElasticClientFactoryOptions> options = null)
        {
            AddElasticClientFactory(services);
            if (options != null)
            {
                services.Configure(name ?? Microsoft.Extensions.Options.Options.DefaultName, options);
            }
            return new DefaultElasticClientBuilder(services, name);
        }

        public static DefaultElasticClientBuilder ConfigureElasticClientLifeTime(this DefaultElasticClientBuilder builder, TimeSpan elasticClientLifeTime)
        {
            builder.Services.Configure<ElasticClientFactoryOptions>(builder.Name,
                option => option.ElasticClientLifeTime = elasticClientLifeTime);
            return builder;
        }

        public static DefaultElasticClientBuilder ConfigureElasticClientUrls(this DefaultElasticClientBuilder builder, string[] urls)
        {
            builder.Services.Configure<ElasticClientFactoryOptions>(builder.Name,
                option => option.Urls = urls);
            return builder;
        }

        public static DefaultElasticClientBuilder ConfigureElasticClientUserName(this DefaultElasticClientBuilder builder, string userName)
        {
            builder.Services.Configure<ElasticClientFactoryOptions>(builder.Name,
                option => option.UserName = userName);
            return builder;
        }

        public static DefaultElasticClientBuilder ConfigureElasticClientPassword(this DefaultElasticClientBuilder builder, string password)
        {
            builder.Services.Configure<ElasticClientFactoryOptions>(builder.Name,
                option => option.PassWord = password);
            return builder;
        }

        public static DefaultElasticClientBuilder ConfigureElasticClientIndex(this DefaultElasticClientBuilder builder, string defaultIndex)
        {
            builder.Services.Configure<ElasticClientFactoryOptions>(builder.Name,
                option => option.DefaultIndex = defaultIndex);
            return builder;
        }
    }
}
