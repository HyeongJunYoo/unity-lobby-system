using System;

namespace Multiplayer.Lobby.Messaging
{
    /// <summary>
    /// 동시성 계약: 모든 Publish/Subscribe/Unsubscribe 호출은 단일 스레드(일반적으로
    /// Unity 메인 스레드)에서 일어난다고 가정한다. 다른 스레드에서 호출하려면 호출 측에서
    /// 동기화하거나 MessageChannelBase 파생 클래스에 lock을 도입해야 한다.
    /// </summary>
    public interface IPublisher<T> { void Publish(T message); }

    /// <summary>
    /// 동시성 계약: 모든 Publish/Subscribe/Unsubscribe 호출은 단일 스레드 가정.
    /// 자세한 내용은 IPublisher의 주석 참고.
    /// </summary>
    public interface ISubscriber<T>
    {
        IDisposable Subscribe(Action<T> handler);
        void Unsubscribe(Action<T> handler);
    }

    public interface IMessageChannel<T> : IPublisher<T>, ISubscriber<T>, IDisposable
    {
        bool IsDisposed { get; }
    }

    public interface IBufferedMessageChannel<T> : IMessageChannel<T>
    {
        bool HasBufferedMessage { get; }
        T BufferedMessage { get; }
    }
}
