using System;
using Core.Configuration.Storage;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Json.Newtonsoft;
using Microsoft.AspNetCore.Mvc;

namespace Core.Configuration.Dashboard
{
    [Route("config/dashboard")]
    public class DashboardActionRoute
    {
        private readonly IConfigurationStorage _configurationStorage;

        public DashboardActionRoute(IConfigurationStorage configurationStorage)
        {
            _configurationStorage = configurationStorage;
        }

        [HttpGet]
        [Route("index.html")]
        public async Task GetIndexAsync(HttpContext context, CancellationToken cancellationToken)
        {
            await using var stream = GetType().Assembly.GetManifestResourceStream("Core.Configuration.Dashboard.wwwroot.html" + ".index.html");
            if (stream == null) throw new InvalidOperationException();

            using var sr = new StreamReader(stream);
            var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
            await context.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8, cancellationToken);
        }

        [HttpGet]
        [Route("all")]
        public async Task GetAllAsync(HttpContext context, CancellationToken cancellationToken)
        {
            var messages = await _configurationStorage.GetAsync(cancellationToken);
            await context.Response.WriteAsync(messages.ToJson(), Encoding.UTF8, cancellationToken);
        }

        [HttpGet]
        [Route("get")]
        public async Task GetAsync(string key, HttpContext context, CancellationToken cancellationToken)
        {
            var message = await _configurationStorage.GetAsync(null, key, cancellationToken);
            await context.Response.WriteAsync(message.ToJson(), Encoding.UTF8, cancellationToken);
        }

        [HttpPost]
        [Route("add")]
        public async Task AddAsync(ConfigurationMessage dto, HttpContext context, CancellationToken cancellationToken)
        {
            var message = new ConfigurationMessage(dto.Key, dto.Value, dto.Description);
            await _configurationStorage.AddAsync(message, cancellationToken);
        }

        [HttpPost]
        [Route("update")]
        public async Task UpdateAsync(ConfigurationMessage message, HttpContext context, CancellationToken cancellationToken)
        {
            await _configurationStorage.UpdateAsync(message, cancellationToken);
        }

        [HttpPost]
        [Route("delete")]
        public async Task DeletedAsync(ConfigurationMessage message, HttpContext context, CancellationToken cancellationToken)
        {
            await _configurationStorage.DeletedAsync(message.Id, cancellationToken);
        }

        internal static List<Method<DashboardActionRoute>> GetMethods()
        {
            var methods = new List<Method<DashboardActionRoute>>
            {
                new Method<DashboardActionRoute>( "GetIndexAsync"),
                new Method<DashboardActionRoute>( "GetAllAsync"),
                new Method<DashboardActionRoute>( "GetAsync", typeof(string)),
                new Method<DashboardActionRoute>( "AddAsync", typeof(ConfigurationMessage)),
                new Method<DashboardActionRoute>( "UpdateAsync", typeof(ConfigurationMessage)),
                new Method<DashboardActionRoute>( "DeletedAsync", typeof(ConfigurationMessage))
            };
            return methods;
        }
    }
}
