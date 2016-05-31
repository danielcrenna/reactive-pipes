namespace reactive.pipes
{
    /// <summary>
    /// A producer of events that intends to send those events to an attached <see cref="IConsume{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IProduce<out T>
    {
        void Attach(IConsume<T> consumer);
    }
}