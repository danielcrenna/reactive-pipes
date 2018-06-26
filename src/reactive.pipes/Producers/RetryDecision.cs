namespace reactive.pipes.Producers
{
    public enum RetryDecision
    {
        RetryImmediately,
        Requeue,
		Backlog,
        Undeliverable,
        Destroy
    }
}