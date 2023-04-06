namespace GameApi.Utils
{
    public class SystemClock : IClock
    {
        private SystemClock() { }

        public static IClock Instance = new SystemClock();
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
