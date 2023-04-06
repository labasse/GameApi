namespace GameApi.Utils
{
    public class SystemGuidGenerator : IGuidGenerator
    {
        private SystemGuidGenerator()
        { }

        public static IGuidGenerator Instance { get; } = new SystemGuidGenerator();

        public Guid NewGuid() => Guid.NewGuid();
    }
}
