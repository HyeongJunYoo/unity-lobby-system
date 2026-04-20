using System;

namespace Multiplayer.Lobby.Abstractions
{
    public interface ITickSource
    {
        event Action OnUpdate;
        event Action OnLateUpdate;
    }
}
