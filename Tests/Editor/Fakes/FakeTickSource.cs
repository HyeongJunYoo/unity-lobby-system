using System;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeTickSource : ITickSource
    {
        public event Action OnUpdate;
        public event Action OnLateUpdate;
        public void TickUpdate()     => OnUpdate?.Invoke();
        public void TickLateUpdate() => OnLateUpdate?.Invoke();
    }
}
