using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Abstractions
{
    public interface IConnectionPayloadSerializer
    {
        byte[] Serialize(ConnectionPayload payload);
        ConnectionPayload Deserialize(byte[] bytes);
    }
}
