namespace reactive.pipes
{
    /// <summary>
    /// A pipe produces on one end and consumers from another.
    /// </summary>
    /// <typeparam name="TP"></typeparam>
    /// <typeparam name="TC"></typeparam>
    public interface IPipe<out TP, in TC> : IProduce<TP>, IConsume<TC>
    {
        
    }
}