public interface IRedisService
{
    Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null);
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry);
    Task<string?> GetAsync(string key);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<long> IncrementAsync(string key);
    Task<bool> ExpireAsync(string key, TimeSpan expiry);
    Task<bool> SetWithAtomicExpireAsync(string key, string value, TimeSpan expiry);
}