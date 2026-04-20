using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeConnectionPayloadSerializer : IConnectionPayloadSerializer
    {
        readonly Dictionary<int, ConnectionPayload> m_Cache = new();
        int m_NextId;

        public byte[] Serialize(ConnectionPayload payload)
        {
            var id = m_NextId++;
            m_Cache[id] = payload;
            return System.BitConverter.GetBytes(id);
        }

        public ConnectionPayload Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return null;
            var id = System.BitConverter.ToInt32(bytes, 0);
            return m_Cache.TryGetValue(id, out var p) ? p : null;
        }
    }
}
