using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedLibrary.Common.Response;

namespace WebApi.Common.Exceptions
{
    public static class GlobalExceptionHandler
    {
        public static void UseGlobalExceptionHandler(this WebApplication app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var env = app.Environment;
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();

                    var feature = context.Features.Get<IExceptionHandlerFeature>();
                    var ex = feature?.Error;

                    if (ex != null)
                        logger.LogError(ex, "Unhandled exception caught by global exception handler.");

                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var response = new ApiResponse
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Success = false,
                        Message = env.IsDevelopment()
                            ? ex?.Message ?? "An unexpected error occurred."
                            : "An unexpected error occurred.",
                        Data = env.IsDevelopment()
                            ? new { Exception = ex?.Message, StackTrace = ex?.StackTrace }
                            : null
                    };

                    await context.Response.WriteAsJsonAsync(response);
                });
            });
        }
    }
}
