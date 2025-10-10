using SecureVault.Interaction.Api.Features.Sync.Contracts;
using System.Collections.Concurrent;

namespace SecureVault.Interaction.Api.Features.Sync.Services
{
    public class InMemoryUserConnectionManager : IUserConnectionManager
    {
        private record Connection(string ConnectionId, string DeviceId);

        private static readonly ConcurrentDictionary<Guid, HashSet<Connection>> _userConnections = new();
        private static readonly ConcurrentDictionary<string, Guid> _connectionUsers = new();

        public void AddConnection(Guid userId, string connectionId, string deviceId)
        {
            var connections = _userConnections.GetOrAdd(userId, _ => []);
            lock (connections)
            {
                connections.Add(new Connection(connectionId, deviceId));
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
                        var connectionToRemove = connections.FirstOrDefault(c => c.ConnectionId == connectionId);
                        if (connectionToRemove is not null)
                            connections.Remove(connectionToRemove);

                        if (!connections.Any())
                            _userConnections.TryRemove(userId, out _);
                    }
                }
                return userId;
            }
            return null;
        }
        public IReadOnlyList<string> GetConnections(Guid userId, string? deviceIdToExclude = null)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    IEnumerable<Connection> query = connections;
                    if (!string.IsNullOrEmpty(deviceIdToExclude))
                        query = query.Where(c => c.DeviceId != deviceIdToExclude);

                    return [.. query.Select(c => c.ConnectionId)];
                }
            }
            return [];
        }
    }
}
