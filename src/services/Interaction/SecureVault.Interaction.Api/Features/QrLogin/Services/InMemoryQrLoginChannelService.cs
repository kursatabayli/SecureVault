using Microsoft.Extensions.Caching.Memory;
using SecureVault.Interaction.Api.Features.QrLogin.Contracts;

namespace SecureVault.Interaction.Api.Features.QrLogin.Services
{
    public class InMemoryQrLoginChannelService : IQrLoginChannelService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _channelExpiration = TimeSpan.FromMinutes(2);

        public InMemoryQrLoginChannelService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<string> CreateChannelAsync()
        {
            var channelId = Guid.NewGuid().ToString();
            _cache.Set(channelId, "Waiting", _channelExpiration);
            return Task.FromResult(channelId);
        }

        public Task<bool> ValidateChannelAsync(string channelId)
        {
            return Task.FromResult(_cache.TryGetValue(channelId, out var state) && state as string == "Waiting");
        }

        public Task MarkChannelAsCompletedAsync(string channelId)
        {
            _cache.Remove(channelId);
            return Task.CompletedTask;
        }
    }
}
