using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    public interface IPublisher<T>
    {
        void Publish(T message);
    }

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
