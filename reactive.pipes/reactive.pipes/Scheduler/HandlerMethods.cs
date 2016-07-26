namespace reactive.pipes.Scheduler
{
    /// <summary>
    /// Stores method hooks. Used as a cache key for running tasks.
    /// </summary>
    internal class HandlerMethods
    {
        internal Before OnBefore { get; set; }
        internal Handler Handler { get; set; }
        internal After OnAfter { get; set; }
        internal Success OnSuccess { get; set; }
        internal Failure OnFailure { get; set; }
        internal Halt OnHalt { get; set; }
        internal Error OnError { get; set; }
    }
}