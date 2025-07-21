namespace Restless.Config
{
    public class PingTarget
    {
        public string Name { get; set; } = default!;
        public string Url { get; set; } = default!;
        public int IntervalSeconds { get; set; } = 300;
        public int MaxRetries { get; set; } = 3;
        public string Email { get; set; } = default!;
    }
}
