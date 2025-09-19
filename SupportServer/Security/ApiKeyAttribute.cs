using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace SupportServer.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string ApiKeyHeaderName = "X-Api-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Resolve ServerDbContext from DI
            var dbContext = context.HttpContext.RequestServices.GetService<ServerDbContext>();
            if (dbContext == null)
            {
                context.Result = new StatusCodeResult(500); // Internal Server Error
                return;
            }

            // Query the database for a valid API key
            var apiKeyExists = await dbContext.ApiAccesses
                .AnyAsync(k => k.Key == (string)extractedApiKey && k.IsActive);

            if (!apiKeyExists)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }
    }
}