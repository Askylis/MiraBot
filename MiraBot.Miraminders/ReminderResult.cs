using MiraBot.DataAccess;

namespace MiraBot.Miraminders
{
    public struct ReminderResult
    {
        public bool IsSuccess {  get; set; }
        public string Message { get; set; }
        public Reminder? Reminder { get; set; }
    }
}
