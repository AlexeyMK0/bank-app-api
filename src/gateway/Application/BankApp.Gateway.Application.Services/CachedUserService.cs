using BankApp.Gateway.Application.Contracts;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace BankApp.Gateway.Application.Services;

public sealed class CachedUserService : IUserService
{
    private readonly IUserService _service;

    private readonly HybridCache _cache;

    private readonly UserCachingOptions _cachingOptions;

    public CachedUserService(IUserService service, HybridCache cache, IOptions<UserCachingOptions> options)
    {
        _service = service;
        _cache = cache;
        _cachingOptions = options.Value;
    }

    public async Task<long> AddUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"userId:{userId.ToString()}";

        var entryOptions = new HybridCacheEntryOptions
        {
            // Аналог AbsoluteExpiration. Принимает TimeSpan.
            Expiration = _cachingOptions.CacheAbsoluteExpirationTime,

            // Настройка времени жизни конкретно для локального L1-кэша (в памяти приложения)
            LocalCacheExpiration = _cachingOptions.CacheSlidingExpirationTime,
        };

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async token => await _service.AddUserAsync(userId, token),
            options: entryOptions,
            cancellationToken: cancellationToken);
    }
}