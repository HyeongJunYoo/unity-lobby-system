namespace Multiplayer.Lobby.Messaging
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
