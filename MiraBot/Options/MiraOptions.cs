namespace MiraBot.Options
{
    public sealed class MiraOptions
    {
        public required int MaxMessageLength { get; set; }
        public required int MaxReminderCount { get; set; }
        public required string DevUserName { get; set; }
        public required ulong DevId { get; set; }
    }
}
