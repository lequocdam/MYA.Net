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
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            logger.LogWarning(ex, "Database unique constraint violation.");
            await WriteResponse(ctx,
                StatusCodes.Status409Conflict,
                "DUPLICATE_RESOURCE",
                "A resource with the same unique value already exists.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict occurred.");

            await WriteResponse(
                context,
                StatusCodes.Status409Conflict,
                "CONCURRENCY_CONFLICT",
                "The resource was modified by another request.");
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