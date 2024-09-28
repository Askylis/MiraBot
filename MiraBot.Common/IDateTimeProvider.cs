namespace MiraBot.Common
{
    public interface IDateTimeProvider
    {
        DateTime Now { get { return DateTime.Now; } }
        DateTime UtcNow { get { return DateTime.UtcNow; } }
        DateTime Today { get { return DateTime.Today; } }
    }
}
