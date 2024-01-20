using System;
using System.Linq;
using Core.PersistentLogging.MvcFilters.PersistentLogging.Model;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public class PersistentLogAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var services = context.HttpContext.RequestServices;
            try
            {
                var options = services.GetService<IOptionsMonitor<ActionFilterPersistentLoggingOptions>>().CurrentValue;

                var storageSourceProvider = services.GetService<IActionFilterPersistentLoggingStorageSourceProvider>();
                if (storageSourceProvider == null)
                    return;

                var storageSourceNames = options.StorageSources.Distinct();
                var httpRequest = context.HttpContext.Request;
                var httpResponse = context.HttpContext.Response;
                var actionNameAttribute = context.Filters.FirstOrDefault(p => p.GetType() == typeof(ActionNameAttribute));

                string actionName = null;
                if (actionNameAttribute != null)
                {
                    actionName = (actionNameAttribute as ActionNameAttribute)!.Name;
                }

                foreach (var storageSourceName in storageSourceNames)
                {
                    var storageSource = storageSourceProvider.GetStorageSource(storageSourceName).GetAwaiter().GetResult();
                    if (storageSource == null)
                        continue;

                    var model = new PersistentLoggingDto(actionName, new HttpRequestDto(httpRequest), new HttpResponseDto(httpResponse));

                    if (context.Exception != null)
                    {
                        model.ActionExecutionException(context.Exception);
                    }

                    storageSource.AddLog(model).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                var logging = services.GetRequiredService<ILogger<PersistentLogAttribute>>();
                logging.LogError($"请求接口-持久化接口日志失败，错误原因：{e.Message}", e);
            }

        }
    }
}
