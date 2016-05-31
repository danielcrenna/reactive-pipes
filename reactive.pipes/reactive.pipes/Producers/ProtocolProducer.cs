using System;
using System.IO;
using System.Threading.Tasks;
using reactive.pipes.Serializers;

namespace reactive.pipes.Producers
{
    /// <summary>
    /// The production end of a pipe that abstracts away the serialization mechanism from downstream consumers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ProtocolProducer<T> : IPipe<Stream, T>
    {
        private readonly ISerializer _serializer;
        private Func<Stream, Task<bool>> _handler;
        
        public ProtocolProducer() : this(new BinarySerializer()) { }
        
        public ProtocolProducer(ISerializer serializer)
        {
            _serializer = serializer;
            _handler = stream => Task.FromResult(true);
        }

        public void Attach(IConsume<Stream> consumer)
        {
            _handler = consumer.HandleAsync;
        }

        public async Task<bool> HandleAsync(T @event)
        {
            return await Task.Run(() =>
            {
                var serialized = _serializer.SerializeToStream(@event);
                _handler(serialized);
                return true;
            });
        }
    }
}