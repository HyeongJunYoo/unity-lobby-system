using System.Text;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class JsonUtilityConnectionPayloadSerializer : IConnectionPayloadSerializer
    {
        public byte[] Serialize(ConnectionPayload payload)
        {
            var json = JsonUtility.ToJson(payload);
            return Encoding.UTF8.GetBytes(json);
        }

        public ConnectionPayload Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var json = Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<ConnectionPayload>(json);
        }
    }
}
