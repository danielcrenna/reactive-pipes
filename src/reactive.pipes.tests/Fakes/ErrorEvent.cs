using reactive.tests.Fakes;

namespace reactive.pipes.tests.Fakes
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