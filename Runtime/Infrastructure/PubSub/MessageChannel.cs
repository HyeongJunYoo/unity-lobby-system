namespace Multiplayer.Lobby.Infrastructure
{
    public class MessageChannel<T> : MessageChannelBase<T>
    {
        public override void Publish(T message)
        {
            ThrowIfDisposed();
            InvokeHandlers(message);
        }
    }
}
