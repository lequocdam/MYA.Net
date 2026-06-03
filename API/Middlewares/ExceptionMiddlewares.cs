using System.Text.Json;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (AppException ex)
        {
            logger.LogWarning("AppException: {Code} - {Message}", ex.ErrorCode, ex.Message);
            await WriteResponse(ctx, ex.StatusCode, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteResponse(ctx, 500, "INTERNAL_ERROR", "An unexpected error occurred");
        }
    }

    private static Task WriteResponse(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = status;
        var body = JsonSerializer.Serialize(new { code, message });
        return ctx.Response.WriteAsync(body);
    }
}