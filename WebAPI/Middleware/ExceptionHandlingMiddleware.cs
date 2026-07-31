
using System.Net;
using System.Text.Json;
namespace WebAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception occured");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new { status = 500, 
                message = "Unexpected error occured, try again.", 
                traceId = context.TraceIdentifier };
                
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            
        }
        
        
    }
    
    
}