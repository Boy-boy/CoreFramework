using System;
using Core.Configuration.Storage;
using Core.Json.SystemTextJson;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.Configuration.Dashboard
{
    [Route("config/dashboard")]
    public class DashboardActionRoute
    {
        private readonly ILogger<DashboardActionRoute> _logger;
        private readonly IConfigurationStorage _configurationStorage;

        public DashboardActionRoute(
            ILogger<DashboardActionRoute> logger,
            IConfigurationStorage configurationStorage)
        {
            _logger = logger;
            _configurationStorage = configurationStorage;
        }

        [HttpPost]
        [Route("all")]
        public async Task GetAllAsync(MessageQueryModel query, HttpContext context, CancellationToken cancellationToken)
        {
            try
            {
                query ??= new MessageQueryModel();
                var messages = await _configurationStorage.GetAsync(query, cancellationToken);
                var apiResult = new ApiResult<PageResultDto<ConfigurationMessage>>(messages);
                await context.Response.WriteAsync(apiResult.ToJson(), Encoding.UTF8, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                var apiResult = new ApiResult<List<ConfigurationMessage>>(500, e.Message);
                await context.Response.WriteAsync(apiResult.ToJson(), Encoding.UTF8, cancellationToken);
            }
        }

        [HttpPost]
        [Route("add")]
        public async Task AddAsync(CreateMessageModel dto, HttpContext context, CancellationToken cancellationToken)
        {
            try
            {
                dto ??= new CreateMessageModel();
                await _configurationStorage.AddAsync(dto, cancellationToken);
                var apiResult = new ApiResult();
                await context.Response.WriteAsync(apiResult.ToJson(), Encoding.UTF8, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                var apiResult = new ApiResult(500, e.Message);
                await context.Response.WriteAsync(apiResult.ToJson(), Encoding.UTF8, cancellationToken);
            }
        }

        [HttpPost]
        [Route("update")]
        public async Task UpdateAsync(ModifyMessageModel dto, HttpContext context, CancellationToken cancellationToken)
        {
            try
            {
                dto ??= new ModifyMessageModel();
                await _configurationStorage.UpdateAsync(dto, cancellationToken);
                var apiResult = new ApiResult();
                await context.Response.WriteAsync(apiResult.ToJson(), Encoding.UTF8, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                var apiResult = new ApiResult(500, e.Message);
                await context.Response.WriteAsync(apiResult.ToJson(), Encoding.UTF8, cancellationToken);
            }
        }

        [HttpDelete]
        [Route("delete")]
        public async Task DeletedAsync(string id, HttpContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (!int.TryParse(id, out var idInt))
                {
                    throw new Exception("the request parameter must be of type int");
                }
                await _configurationStorage.DeletedAsync(idInt, cancellationToken);
                var apiResult = new ApiResult();
                await context.Response.WriteAsync(apiResult.ToJson(), Encoding.UTF8, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                var apiResult = new ApiResult(500, e.Message);
                await context.Response.WriteAsync(apiResult.ToJson(), Encoding.UTF8, cancellationToken);
            }
        }

        internal static List<Method<DashboardActionRoute>> GetMethods()
        {
            var methods = new List<Method<DashboardActionRoute>>
            {
                new Method<DashboardActionRoute>( "GetAllAsync",typeof(MessageQueryModel)),
                new Method<DashboardActionRoute>( "AddAsync", typeof(CreateMessageModel)),
                new Method<DashboardActionRoute>( "UpdateAsync", typeof(ModifyMessageModel)),
                new Method<DashboardActionRoute>( "DeletedAsync", typeof(string))
            };
            return methods;
        }
    }

    /// <summary>
    /// API 响应结果
    /// </summary>
    public class ApiResult
    {
        public ApiResult()
        {
            Success = true;
            ErrorCode = 0;
        }

        public ApiResult(int errorCode, string message = null)
        {
            ErrorCode = errorCode;
            Message = message;
            Success = false;
        }

        /// <summary>
        /// API 执行是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// ErrorCode 为 0 表示执行无异常
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 当API执行有异常时, 对应的错误信息
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Api返回结果
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class ApiResult<TResult> : ApiResult
    {
        public ApiResult()
        {
        }

        public ApiResult(TResult result)
            : this()
        {
            Result = result;
        }

        public ApiResult(int errorCode, string message = null)
            : base(errorCode, message) { }

        /// <summary>
        /// API执行返回的结果
        /// </summary>
        public TResult Result { get; set; }
    }
}
