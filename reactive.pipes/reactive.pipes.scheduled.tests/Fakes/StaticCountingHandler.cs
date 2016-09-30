namespace reactive.pipes.scheduled.tests.Fakes
{
    public class StaticCountingHandler
    {
        public static int Count { get; set; }

        public string SomeOption { get; set; }

        public bool Perform()
        {
            Count++;
            return true;
        }
    }
}
