using System;

namespace Multiplayer.Lobby.Messaging
{
    public class BufferedMessageChannel<T> : MessageChannelBase<T>, IBufferedMessageChannel<T>
    {
        public bool HasBufferedMessage { get; private set; }
        public T BufferedMessage { get; private set; }

        public override void Publish(T message)
        {
            ThrowIfDisposed();

            HasBufferedMessage = true;
            BufferedMessage = message;

            InvokeHandlers(message);
        }

        public override IDisposable Subscribe(Action<T> handler)
        {
            var subscription = base.Subscribe(handler);

            // Deliver the buffered message immediately to new subscribers
            if (HasBufferedMessage)
            {
                handler?.Invoke(BufferedMessage);
            }

            return subscription;
        }

        public override void Dispose()
        {
            if (!IsDisposed)
            {
                HasBufferedMessage = false;
                BufferedMessage = default;
            }
            base.Dispose();
        }
    }
}
