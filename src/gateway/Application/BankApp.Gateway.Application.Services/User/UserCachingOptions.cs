using System.ComponentModel.DataAnnotations;

namespace BankApp.Gateway.Application.Services.User;

public sealed class UserCachingOptions
{
    [Range(typeof(TimeSpan), "00:00:00.100", maximum: "00:30:00")]
    public TimeSpan CacheAbsoluteExpirationTime { get; set; }

    [Range(typeof(TimeSpan), "00:00:00.100", maximum: "00:05:00")]
    public TimeSpan CacheSlidingExpirationTime { get; set; }
}