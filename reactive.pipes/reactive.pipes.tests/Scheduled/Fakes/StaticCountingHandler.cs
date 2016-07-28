namespace reactive.tests.Scheduled.Fakes
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
