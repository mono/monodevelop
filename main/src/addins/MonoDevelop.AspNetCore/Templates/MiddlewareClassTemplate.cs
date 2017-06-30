using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ${Namespace}
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ${EscapedIdentifier}
    {
        private readonly RequestDelegate _next;

        public ${EscapedIdentifier}(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {

            return _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ${EscapedIdentifier}Extensions
    {
        public static IApplicationBuilder UseMiddlewareClassTemplate(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<${EscapedIdentifier}>();
        }
    }
}
