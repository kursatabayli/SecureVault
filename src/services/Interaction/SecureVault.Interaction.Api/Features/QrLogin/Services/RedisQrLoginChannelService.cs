using SecureVault.Interaction.Api.Features.QrLogin.Contracts;
using StackExchange.Redis;

namespace SecureVault.Interaction.Api.Features.QrLogin.Services;

public class RedisQrLoginChannelService : IQrLoginChannelService
{
    private readonly IDatabase _redisDb;
    private readonly ILogger<RedisQrLoginChannelService> _logger;
    private readonly TimeSpan _channelExpiration = TimeSpan.FromMinutes(1);

    public RedisQrLoginChannelService(IConnectionMultiplexer redis, ILogger<RedisQrLoginChannelService> logger)
    {
        _redisDb = redis.GetDatabase();
        _logger = logger;
    }

    private string GetChannelStateKey(string channelId) => $"qr:channel:{channelId}:state";
    private string GetChannelCountKey(string channelId) => $"qr:channel:{channelId}:count";
    private string GetConnectionKey(string connectionId) => $"qr:conn:{connectionId}";

    public async Task<(bool Success, int NewCount, string Role)> RegisterConnectionAsync(string channelId, string connectionId)
    {
        var stateKey = GetChannelStateKey(channelId);
        var countKey = GetChannelCountKey(channelId);
        var connKey = GetConnectionKey(connectionId);

        _logger.LogInformation("RegisterConnectionAsync: {ConnectionId} -> {ChannelId}", connectionId, channelId);

        var tranCreator = _redisDb.CreateTransaction();
        tranCreator.AddCondition(Condition.KeyNotExists(stateKey));

        tranCreator.StringSetAsync(stateKey, "Waiting", _channelExpiration);
        tranCreator.StringSetAsync(countKey, 1, _channelExpiration);
        tranCreator.StringSetAsync(connKey, channelId, _channelExpiration);

        try
        {
            if (await tranCreator.ExecuteAsync())
            {
                _logger.LogInformation("RegisterConnectionAsync: NEW channel created (Creator). {ChannelId}, Count=1", channelId);
                return (true, 1, "Creator");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterConnectionAsync (Creator Path): Transaction ERROR. {ChannelId}", channelId);
            return (false, -1, null);
        }

        _logger.LogInformation("RegisterConnectionAsync: Channel exists, attempting to join (Joiner). {ChannelId}", channelId);

        if (!await ValidateChannelAsync(channelId))
        {
            _logger.LogWarning("RegisterConnectionAsync (Joiner Path): Channel is not valid (not 'Waiting' or expired). {ChannelId}", channelId);
            return (false, -1, "Invalid");
        }

        var tranJoiner = _redisDb.CreateTransaction();
        tranJoiner.AddCondition(Condition.StringEqual(countKey, 1));

        tranJoiner.StringSetAsync(connKey, channelId, _channelExpiration);
        var newCountTask = tranJoiner.StringIncrementAsync(countKey);
        tranJoiner.KeyExpireAsync(connKey, _channelExpiration);
        tranJoiner.KeyExpireAsync(countKey, _channelExpiration);
        tranJoiner.KeyExpireAsync(stateKey, _channelExpiration);

        try
        {
            if (await tranJoiner.ExecuteAsync())
            {
                var finalCount = (int)await newCountTask;
                _logger.LogInformation("RegisterConnectionAsync: Joiner joined. {ChannelId}, New Count={NewCount}", channelId, finalCount);
                return (true, finalCount, "Joiner");
            }
            else
            {
                var currentCount = await _redisDb.StringGetAsync(countKey);
                _logger.LogWarning("RegisterConnectionAsync (Joiner Path): Transaction FAILED. Condition (count == 1) not met. Current Count: {Count}", currentCount.HasValue ? currentCount.ToString() : "NULL");
                return (false, (int)(currentCount.HasValue ? currentCount : -1), "BusyOrFull");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterConnectionAsync (Joiner Path): Transaction ERROR. {ChannelId}", channelId);
            return (false, -1, null);
        }
    }

    public async Task<bool> ValidateChannelAsync(string channelId)
    {
        var key = GetChannelStateKey(channelId);
        _logger.LogInformation("ValidateChannelAsync: Validating channel. Key: {Key}", key);

        try
        {
            var state = await _redisDb.StringGetAsync(key);

            if (!state.HasValue)
            {
                _logger.LogWarning("ValidateChannelAsync: Key not found or expired. Key: {Key}", key);
                return false;
            }

            bool isValid = state == "Waiting";
            _logger.LogInformation("ValidateChannelAsync: Channel state '{State}'. IsValid: {IsValid}", state.ToString(), isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateChannelAsync: Error occurred during validation. Key: {Key}", key);
            return false;
        }
    }

    public async Task MarkChannelAsCompletedAsync(string channelId)
    {
        var keys = new RedisKey[] {
            GetChannelStateKey(channelId),
            GetChannelCountKey(channelId)
        };
        _logger.LogInformation("MarkChannelAsCompletedAsync: Deleting keys for {ChannelId}.", channelId);
        try
        {
            await _redisDb.KeyDeleteAsync(keys);
            _logger.LogInformation("MarkChannelAsCompletedAsync: Successfully deleted keys for {ChannelId}.", channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkChannelAsCompletedAsync: ERROR during deletion. ChannelId: {ChannelId}", channelId);
        }
    }

    public async Task<string?> LeaveChannelAsync(string connectionId)
    {
        _logger.LogInformation("LeaveChannelAsync: {ConnectionId} is leaving.", connectionId);
        var connKey = GetConnectionKey(connectionId);

        try
        {
            var channelId = await _redisDb.StringGetDeleteAsync(connKey);

            if (!channelId.HasValue)
            {
                _logger.LogWarning("LeaveChannelAsync: No channel found for {ConnectionId} (perhaps expired).", connectionId);
                return null;
            }

            var countKey = GetChannelCountKey(channelId.ToString());

            var newCount = await _redisDb.StringDecrementAsync(countKey);
            _logger.LogInformation("LeaveChannelAsync: {ConnectionId} left. Channel: {ChannelId}, New Count: {NewCount}", connectionId, channelId.ToString(), newCount);

            return channelId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LeaveChannelAsync: ERROR occurred. ConnectionId: {ConnectionId}", connectionId);
            return null;
        }
    }
}