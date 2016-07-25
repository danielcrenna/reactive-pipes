namespace reactive.tests.Scheduled.Fakes
{
    public class CountingHandler
    {
        public static int Count { get; set; }

        public bool Perform()
        {
            Count++;
            return true;
        }
    }
}
