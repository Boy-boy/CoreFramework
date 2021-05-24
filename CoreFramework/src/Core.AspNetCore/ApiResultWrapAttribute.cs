using System;
using Core.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Core.AspNetCore
{
    /// <summary>
    /// API 相应结果包装器
    /// </summary>
    public class ApiResultWrapAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// action执行后执行
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                if (context.Exception is DomainException domainException)
                {
                    context.Result = new JsonResult(new ApiResult(domainException.ErrorCode, domainException.Message));
                }
                else
                {
                    context.Result = new JsonResult(new ApiResult(500, context.Exception.Message));
                }

                var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<ApiResultWrapAttribute>)) as ILogger<ApiResultWrapAttribute>;
                logger?.LogError("request action error,message body is {message}", context.Exception.Message);
            }
            else
            {
                var actionResult = GetValue(context.Result);
                if (actionResult == null)
                {
                    context.Result = new JsonResult(new ApiResult());
                }
                else
                {
                    var resultType = typeof(ApiResult<>).MakeGenericType(actionResult.GetType());
                    context.Result = new JsonResult(Activator.CreateInstance(resultType, actionResult));
                }
            }
        }

        /// <summary>
        /// 获取actionResult
        /// </summary>
        /// <param name="actionResult"></param>
        /// <returns></returns>
        private object GetValue(IActionResult actionResult)
        {
            return (actionResult as JsonResult)?.Value ?? (actionResult as ObjectResult)?.Value;
        }
    }
}