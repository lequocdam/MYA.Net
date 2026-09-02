public class IdempotencyMiddleware
{
    private readonly RequestDelegate next;
    private readonly IConnectionMultiplexer redis;
    private readonly IdempotencyOptions options;
    private readonly ILogger<IdempotencyMiddleware> logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        IOptions<IdempotencyOptions> options,
        ILogger<IdempotencyMiddleware> logger)
    {
        this.next = next;
        this.redis = redis;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            && !HttpMethods.IsPut(context.Request.Method)
            && !HttpMethods.IsPatch(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(options.HeaderName, out var keyValue)
            || string.IsNullOrWhiteSpace(keyValue))
        {
            if (options.RequireHeader)
            {
                await WriteErrorAsync(context, $"Thiếu header {options.HeaderName}.");
                return;
            }
            await next(context); // cho phép đi tiếp nếu header optional
            return;
        }

        var idempotencyKey = keyValue.ToString();

        if (!Guid.TryParse(idempotencyKey, out _))
        {
            await WriteErrorAsync(context, $"{options.HeaderName} phải là GUID hợp lệ.");
            return;
        }

        var db = redis.GetDatabase();
        var responseCacheKey = $"idem:response:{idempotencyKey}";
        var lockKey = $"idem:lock:{idempotencyKey}";

        // Bước 1: check đã có kết quả cache từ trước chưa — trả ngay, KHÔNG chạy action
        var cached = await db.StringGetAsync(responseCacheKey);
        if (!cached.IsNullOrEmpty)
        {
            await WriteCachedResponseAsync(context, cached!);
            logger.LogInformation("Idempotent replay for key {Key}", idempotencyKey);
            return;
        }

        // Bước 2: cố gắng acquire lock — chặn request khác đang xử lý CÙNG key này
        var lockToken = Guid.NewGuid().ToString();
        var acquired = await db.StringSetAsync(
            lockKey, lockToken, options.LockDuration, When.NotExists);

        if (!acquired)
        {
            // Có request khác đang xử lý key này CÙNG lúc → từ chối ngay, không cho chạy song song
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiResponse<object>
            {
                Message = "Request với Idempotency-Key này đang được xử lý, vui lòng thử lại sau."
            }));
            return;
        }

        try
        {
            // Double-check sau khi có lock — phòng trường hợp request khác vừa xử lý xong
            // đúng lúc mình đang acquire lock (hiếm nhưng có thể xảy ra)
            cached = await db.StringGetAsync(responseCacheKey);
            if (!cached.IsNullOrEmpty)
            {
                await WriteCachedResponseAsync(context, cached!);
                return;
            }

            // Bước 3: capture response để cache lại sau khi action xử lý xong
            var originalBodyStream = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await next(context); // action thực sự chạy ở đây (controller → service)

            memStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memStream).ReadToEndAsync();

            // Chỉ cache response THÀNH CÔNG (2xx) — không cache lỗi tạm thời (5xx),
            // để request retry sau đó có cơ hội thử lại thật sự thay vì bị "khóa cứng" vào lỗi cũ
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                await db.StringSetAsync(responseCacheKey, responseBody, options.CacheDuration);
            }

            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
        finally
        {
            await ReleaseLockAsync(db, lockKey, lockToken);
        }
    }

    private async Task WriteCachedResponseAsync(HttpContext context, string cachedBody)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        context.Response.Headers["X-Idempotent-Replay"] = "true"; // cho FE biết đây là response replay, hữu ích để debug
        await context.Response.WriteAsync(cachedBody);
    }

    private async Task WriteErrorAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiResponse<object>
        {
            Message = message
        }));
    }

    private async Task ReleaseLockAsync(IDatabase db, string lockKey, string lockToken)
    {
        const string script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        try
        {
            await db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockToken });
        }
        catch (Exception ex)
        {
            // Không throw — lock sẽ tự hết hạn theo TTL, không cần fail cả request vì lỗi release
            logger.LogWarning(ex, "Failed to release idempotency lock {LockKey}", lockKey);
        }
    }
}