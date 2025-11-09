using SecureVault.Interaction.Api.Features.QrLogin.Contracts;
using StackExchange.Redis;

namespace SecureVault.Interaction.Api.Features.QrLogin.Services;

public class RedisQrLoginChannelService : IQrLoginChannelService
{
    private readonly IDatabase _redisDb;
    private readonly TimeSpan _channelExpiration = TimeSpan.FromMinutes(1);

    public RedisQrLoginChannelService(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    private string GetChannelStateKey(string channelId) => $"qr:channel:{channelId}:state";
    private string GetChannelCountKey(string channelId) => $"qr:channel:{channelId}:count";
    private string GetConnectionKey(string connectionId) => $"qr:conn:{connectionId}";

    public async Task<string> CreateChannelAsync()
    {
        var channelId = Guid.NewGuid().ToString();
        var key = GetChannelStateKey(channelId);
        await _redisDb.StringSetAsync(key, "Waiting", _channelExpiration);
        return channelId;
    }

    public async Task<bool> ValidateChannelAsync(string channelId)
    {
        var key = GetChannelStateKey(channelId);
        var state = await _redisDb.StringGetAsync(key);
        return state == "Waiting";
    }

    public async Task<int> GetChannelCountAsync(string channelId)
    {
        var countKey = GetChannelCountKey(channelId);
        var countVal = await _redisDb.StringGetAsync(countKey);

        if (countVal.TryParse(out int count))
        {
            return count;
        }

        return 0;
    }

    public async Task MarkChannelAsCompletedAsync(string channelId)
    {
        await _redisDb.KeyDeleteAsync([
            GetChannelStateKey(channelId),
                GetChannelCountKey(channelId)
        ]);
    }

    public async Task<int> JoinChannelAsync(string channelId, string connectionId)
    {
        var connKey = GetConnectionKey(connectionId);
        await _redisDb.StringSetAsync(connKey, channelId);

        var countKey = GetChannelCountKey(channelId);
        var newCount = await _redisDb.StringIncrementAsync(countKey);

        await _redisDb.KeyExpireAsync(connKey, _channelExpiration);
        await _redisDb.KeyExpireAsync(countKey, _channelExpiration);

        return (int)newCount;
    }

    public async Task<string?> LeaveChannelAsync(string connectionId)
    {
        var connKey = GetConnectionKey(connectionId);
        var channelId = await _redisDb.StringGetDeleteAsync(connKey);

        if (!channelId.HasValue)
            return null;

        var countKey = GetChannelCountKey(channelId.ToString());
        await _redisDb.StringDecrementAsync(countKey);

        return channelId.ToString();
    }
}
