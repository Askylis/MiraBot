namespace MiraBot.Miraminders
{
    public sealed class ReminderOptions
    {
        public required int MaxMessageLength { get; set; }
        public required int MaxReminderCount { get; set; }
        public required string DevUserName { get; set; }
    }
}
