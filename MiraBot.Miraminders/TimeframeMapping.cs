namespace MiraBot.Miraminders
{
    internal static class TimeframeMapping
    {
        internal static readonly Dictionary<string, TimeFrame> timeFrameMapping = new()
            {
                { "sec", TimeFrame.Seconds},
                { "secs", TimeFrame.Seconds},
                { "second", TimeFrame.Seconds },
                { "seconds", TimeFrame.Seconds },
                { "minute", TimeFrame.Minutes },
                { "minutes", TimeFrame.Minutes },
                { "min", TimeFrame.Minutes },
                { "mins", TimeFrame.Minutes },
                { "hour", TimeFrame.Hours },
                { "hours", TimeFrame.Hours },
                { "hr", TimeFrame.Hours },
                { "hrs", TimeFrame.Hours },
                { "day", TimeFrame.Days },
                { "days", TimeFrame.Days },
                { "tomorrow", TimeFrame.Days },
                { "week", TimeFrame.Weeks },
                { "weeks", TimeFrame.Weeks },
                { "month", TimeFrame.Months },
                { "months", TimeFrame.Months },
                { "year", TimeFrame.Years },
                { "years", TimeFrame.Years },
                { "yrs", TimeFrame.Years},
                { "yr", TimeFrame.Years}
            };
    }
}