namespace GameApi.Utils
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
