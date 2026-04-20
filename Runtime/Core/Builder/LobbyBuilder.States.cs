using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.StateMachine;
using Multiplayer.Lobby.States;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        readonly Dictionary<Type, Func<IStateMachineContext, ConnectionState>> m_StateFactories = new();

        public LobbyBuilder UseDefaultStates()
        {
            AddState(ctx => new OfflineState(ctx));
            AddState(ctx => new StartingHostState(ctx));
            AddState(ctx => new HostingState(ctx));
            AddState(ctx => new ClientConnectingState(ctx));
            AddState(ctx => new ClientConnectedState(ctx));
            AddState(ctx => new ClientReconnectingState(ctx));
            return this;
        }

        public LobbyBuilder AddState<TState>(Func<IStateMachineContext, TState> factory)
            where TState : ConnectionState
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (m_StateFactories.ContainsKey(typeof(TState)))
                throw new InvalidOperationException(
                    $"상태 {typeof(TState).Name}이 이미 등록되었습니다. ReplaceState<{typeof(TState).Name}>를 사용하십시오.");
            m_StateFactories[typeof(TState)] = ctx => factory(ctx);
            return this;
        }

        public LobbyBuilder ReplaceState<TState>(Func<IStateMachineContext, TState> factory)
            where TState : ConnectionState
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            m_StateFactories[typeof(TState)] = ctx => factory(ctx);
            return this;
        }
    }
}
