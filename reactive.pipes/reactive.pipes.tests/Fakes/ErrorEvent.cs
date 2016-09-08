namespace reactive.tests.Fakes
{
    public class ErrorEvent : BaseEvent
    {
        public bool Error { get; set; }

        public ErrorEvent()
        {
            Error = true;
        }
    }
}