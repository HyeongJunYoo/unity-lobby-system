using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.StateMachine
{
    /// <summary>
    /// 상태 머신의 기본 상태. Task 15에서 가상 메서드들이 추가된다.
    /// </summary>
    public abstract class ConnectionState
    {
        protected IStateMachineContext Context { get; }

        protected ConnectionState(IStateMachineContext context)
        {
            Context = context ?? throw new System.ArgumentNullException(nameof(context));
        }
    }
}
