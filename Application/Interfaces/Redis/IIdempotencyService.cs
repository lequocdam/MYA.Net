public interface IIdempotencyService
{
    Task<TResponse?> GetAsync<TResponse>(string key, CancellationToken ct);
    Task AddAsync<TResponse>(string key, TResponse response, CancellationToken ct);
}