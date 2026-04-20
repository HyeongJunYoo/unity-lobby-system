using System;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class MonoBehaviourTickSource : MonoBehaviour, ITickSource
    {
        public event Action OnUpdate;
        public event Action OnLateUpdate;
        void Update()     => OnUpdate?.Invoke();
        void LateUpdate() => OnLateUpdate?.Invoke();
    }
}
