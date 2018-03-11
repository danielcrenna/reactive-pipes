using System.IO;

namespace reactive.pipes
{
    /// <summary>
    /// A contract for protocol negotiation between produces and consumers.
    /// </summary>
    public interface ISerializer 
    {
        Stream SerializeToStream<T>(T @event);
        T DeserializeFromStream<T>(Stream stream);
    }
}