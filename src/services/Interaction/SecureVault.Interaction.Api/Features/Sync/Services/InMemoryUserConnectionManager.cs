using SecureVault.Interaction.Api.Features.Sync.Contracts;
using System.Collections.Concurrent;

namespace SecureVault.Interaction.Api.Features.Sync.Services
{
    public class InMemoryUserConnectionManager : IUserConnectionManager
    {
        private static readonly ConcurrentDictionary<Guid, HashSet<string>> _userConnections = new();
        private static readonly ConcurrentDictionary<string, Guid> _connectionUsers = new();

        public void AddConnection(Guid userId, string connectionId)
        {
            var connections = _userConnections.GetOrAdd(userId, _ => []);
            lock (connections)
            {
                connections.Add(connectionId);
            }
            _connectionUsers.TryAdd(connectionId, userId);
        }

        public Guid? RemoveConnection(string connectionId)
        {
            if (_connectionUsers.TryRemove(connectionId, out var userId))
            {
                if (_userConnections.TryGetValue(userId, out var connections))
                {
                    lock (connections)
                    {
                        connections.Remove(connectionId);
                        if (!connections.Any())
                        {
                            _userConnections.TryRemove(userId, out _);
                        }
                    }
                }
                return userId;
            }
            return null;
        }
        public IReadOnlyList<string> GetConnections(Guid userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    return [.. connections];
                }
            }
            return [];
        }
    }
}
