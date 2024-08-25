using Discord.WebSocket;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MiraBot.Miraminders
{
    public class ReminderHandler
    {
        private readonly IMiramindersRepository _repository;
        private readonly DiscordSocketClient _client;
        private readonly MiraminderService _service;
        private readonly TimeOnly defaultTime = new(17, 0);
        private List<string> completeInput;
        private static readonly string[] keywords = ["in", "on", "at", "every", "to", "that", "and", "from", "now", "a", "next"];
        private const int maxMessageLength = 50;
        public ReminderHandler(
            IMiramindersRepository repository,
            DiscordSocketClient client,
            MiraminderService service)
        {
            _repository = repository;
            _client = client;
            _service = service;
        }

        // This is almost done. It can handle most types of reminders. It can't handle reminders like the following:
        // remind me every Monday and Friday to do this thing
        // remind me every other Thursday to do this thing
        // remind me next Friday to complete this task 
        // I'll update it to handle reminders like that soon
        // I added TimeFrame.cs and TimeFrameMapping.cs to handle all sorts of timeframes
        public async Task ParseReminderAsync(string input, ulong ownerId)
        {
            // splits the input into a list for easier management
            completeInput = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var owner = await _repository.GetUserByDiscordIdAsync(ownerId);

            // makes new reminder that can be modified
            var reminder = new Reminder { OwnerId = owner.UserId };

            // extract the intended recipient from the reminder input 
            reminder.RecipientId = await GetRecipientIdAsync(completeInput, owner.UserId);

            // search for keyword "every" to determine whether or not this reminder is recurring
            reminder.IsRecurring = IsRecurringReminder();

            // extract the intended date and time from the reminder input
            var dateTime = await GetUserSpecifiedDateTimeAsync(owner, reminder);
            if (dateTime is null)
            {
                await SendMessageAsync("Couldn't find a valid date or time in your reminder input, so I can't build your reminder.", owner.DiscordId);
                return;
            }
            reminder.DateTime = (DateTime)dateTime;
            CleanReminderMessage();
            if (completeInput.Count == 0)
            {
                reminder.Message = "No attached message.";
            }
            else
            {
                reminder.Message = string.Join(" ", completeInput);
            }

            if (reminder.Message.Length > 50)
            {
                await SendMessageAsync($"I couldn't save your reminder because the attached message is too long. Max length for reminder messages is {maxMessageLength} characters, but your message contained {reminder.Message.Length} characters.", owner.DiscordId);
                return;
            }

            try
            {
                await _repository.AddReminderAsync(reminder);
            }
            catch
            {
                await SendMessageAsync("I couldn't add your reminder. It either contained typos, was missing information, or was too complex for me. Sorry!", ownerId);
                return;
            }
            Console.WriteLine($"reminder.RecipientId is \"{reminder.RecipientId}\".");
            Console.WriteLine($"reminder.DateTime is \"{reminder.DateTime}\".");
            Console.WriteLine($"reminder.Message is \"{reminder.Message}\".");
            Console.WriteLine($"reminder.IsRecurring is \"{reminder.IsRecurring}\".");
            Console.WriteLine($"InSeconds: {reminder.InSeconds}");
            Console.WriteLine($"InMinutes: {reminder.InMinutes}");
            Console.WriteLine($"InHours: {reminder.InHours}");
            Console.WriteLine($"InDays: {reminder.InDays}");
            Console.WriteLine($"InWeeks: {reminder.InWeeks}");
            Console.WriteLine($"InMonths: {reminder.InMonths}");
            Console.WriteLine($"InYears: {reminder.InYears}");
            await SendMessageAsync("Got it, your reminder has been saved!", owner.DiscordId);
        }

        private async Task<DateTime?> GetUserSpecifiedDateTimeAsync(User owner, Reminder reminder)
        {
            TimeOnly? time = null;

            // try to get a date from the input, from either a date that's spelled out (August 17th), or from a 
            // numeric value (8/17)
            var date = GetDateFromString(owner) ?? GetDateFromInput(owner.UsesAmericanDateFormat.Value);

            // after attempting to get a date, regardless if the date is null or not, try to get a time.
            DateTime? dateTime = GetSpecifiedTimeAsync(owner);

            if (dateTime is not null)
            {
                // if GetSpecifiedTimeAsync() returned a value, then extract the time from it
                time = TimeOnly.FromDateTime(dateTime.Value);
                if (date is not null)
                {
                    // combines date and time into a usable DateTime
                    return date.Value.ToDateTime(time.Value);
                }
            }

            else if (date is null)
            {
                // if a valid date wasn't detected, then try to see if a reminder was specified for X amount of time from now 
                dateTime = GetTimeFromNow(reminder);
                if (dateTime is null)
                {
                    // if everything is null up to this point, then a valid date and/or time wasn't found within the input
                    return null;
                }
            }

            else
            {
                await SendMessageAsync($"You either didn't enter a time at all, or entered an invalid time. Falling back to {defaultTime} default time.", owner.DiscordId);
                time = GetDefaultUtcTime(owner);
            }

            if (date.HasValue && time.HasValue)
            {
                return date.Value.ToDateTime(time.Value);
            }

            return dateTime != DateTime.MinValue ? dateTime : null;
        }

        private async Task<int> GetRecipientIdAsync(List<string> words, int ownerId)
        {
            // check to see if the first word is "me". If it is, then it's faster because it doesn't require using the database
            // for every word in the reminder input.
            if (words[0].Equals("me", StringComparison.OrdinalIgnoreCase))
            {
                completeInput.RemoveAt(0);
                return ownerId;
            }
            // if the first word isn't "me", then run each word in the reminder input against the database to see if it contains a user

            int recipientIndex = 0;
            foreach (var word in words)
            {
                var recipient = await _repository.GetUserByNameAsync(word);
                if (recipient is not null)
                {
                    completeInput.RemoveAt(recipientIndex);
                    return recipient.UserId;
                }
                recipientIndex++;
            }
            // if neither is true, then return the ownerId since a recipient couldn't be found
            return ownerId;
        }


        private DateOnly? GetDateFromString(User owner)
        {
            var formatInfo = new DateTimeFormatInfo();
            var months = formatInfo.MonthNames.Select(m => m.ToLower()).ToArray();
            int index = -1;
            for (var i = 0; i < completeInput.Count; i++)
            {
                var input = completeInput[i].ToLower();
                if (months.Contains(input))
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                return null;
            }
            string inputs = owner.UsesAmericanDateFormat.Value
            ? $"{completeInput[index]} {completeInput[index + 1]}"
            : $"{completeInput[index]} {completeInput[index - 1]}";


            var cleanedInputs = RemoveDateSuffix(inputs);
            if (DateOnly.TryParse(cleanedInputs, out var date))
            {
                int removeIndex = owner.UsesAmericanDateFormat.Value ? index + 1 : index - 1;
                completeInput.RemoveAt(removeIndex);
                completeInput.RemoveAt(index);
                return date;
            }

            return null;
        }

        private DateOnly? GetDateFromInput(bool isAmerican)
        {
            DateOnly? date = null;
            string[] dateFormats = isAmerican
                ? new[] { "M/d", "M-d", "M.d", "M/d/yyyy", "M-d-yyyy", "M.d.yyyy" }
                : ["d/M", "d-M", "d.M", "d/M/yyyy", "d-M-yyyy", "d.M.yyyy"];


            var index = 0;
            foreach (var word in completeInput)
            {
                if (DateOnly.TryParseExact(word, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    date = parsedDate;
                    completeInput.RemoveAt(index);
                    break;
                }
                index++;
            }

            return date;
        }

        private DateTime? GetSpecifiedTimeAsync(User owner)
        {
            TimeOnly time = new();
            var index = 0;
            var timeIndex = -1;
            List<int> removeRange = new();
            
            foreach (var word in completeInput)
            {
                if (TimeOnly.TryParse(word, out time))
                {
                    // if it can parse an element in completeInput, then it searches for AM/PM
                    if (index != completeInput.Count - 1 && completeInput[index + 1].Equals("AM", StringComparison.OrdinalIgnoreCase) || completeInput[index + 1].Equals("PM", StringComparison.OrdinalIgnoreCase))
                    {
                        // if it finds AM/PM, then it combines the time and AM/PM together in the same element, removes the element where
                        // AM/PM was, and then parses that. 
                        timeIndex = index;
                        removeRange.Add(index + 1);
                    }
                    // if AM/PM isn't found, then the time is assumed to be in 12h time and the element that can be parsed is removed. 
                    removeRange.Add(index);
                }
                index++;
            }
            // I don't really remember why I have this separated out into an if-else statement...
            if (timeIndex > -1)
            {
                completeInput[timeIndex] = $"{completeInput[timeIndex]}{completeInput[timeIndex + 1]}";
                TimeOnly.TryParse(completeInput[timeIndex], out time);
                removeRange = removeRange.OrderByDescending(x => x).ToList();
                foreach (var element in removeRange)
                {
                    completeInput.RemoveAt(element);
                }
            }
            else
            {
                foreach (var element in removeRange)
                {
                    completeInput.RemoveAt(element);
                }
            }
            
            // if time is still the default value, then this method was not able to parse anything from completeInput, 
            // and therefore should return null. 
            if (time == TimeOnly.MinValue)
            {
                return null;
            }
            // if it's not the default value, then a valid time was found and needs to be converted to UTC and returned. 
            var utcTime = _service.ConvertUserTimeToUtc(time, owner.Timezone);
            if (utcTime < DateTime.UtcNow)
            {
                utcTime = utcTime.AddDays(1);
            }
            return utcTime;
        }

        private DateTime GetTimeFromNow(Reminder reminder)
        {
            var dateTime = DateTime.UtcNow;
            TimeFrame result = new();
            int timeframeValue = -1;
            bool hasMatch = false;
            bool hasTimeFrame = false;
            int index = 0;
            do
            {
                hasMatch = false;
                foreach (var word in completeInput)
                {
                    if (TimeframeMapping.timeFrameMapping.TryGetValue(word, out result))
                    {
                        hasMatch = true;
                        hasTimeFrame = true;
                        break;
                    }
                    index++;
                }
                if (index >= completeInput.Count)
                {
                    hasMatch = false;
                }
                if (hasMatch && index > 0 && int.TryParse(completeInput[index - 1], out timeframeValue))
                {
                    completeInput.RemoveAt(index);
                    completeInput.RemoveAt(index - 1);
                }
                else if (hasMatch)
                {
                    timeframeValue = 1;
                    completeInput.RemoveAt(index);
                }

                // fill in time frame column for reminder even if the reminder isn't recurring. 
                // lets this code be used for recurring and one-off reminders, and doesn't 
                // affect one-off reminders. also lets one-off reminders be changed to recurring
                // later. 
                if (hasMatch)
                {
                    switch (result)
                    {
                        case TimeFrame.Seconds:
                            dateTime = dateTime.AddSeconds(timeframeValue);
                            reminder.InSeconds = timeframeValue;
                            break;
                        case TimeFrame.Minutes:
                            dateTime = dateTime.AddMinutes(timeframeValue);
                            reminder.InMinutes = timeframeValue;
                            break;
                        case TimeFrame.Hours:
                            dateTime = dateTime.AddHours(timeframeValue);
                            reminder.InHours = timeframeValue;
                            break;
                        case TimeFrame.Days:
                            dateTime = dateTime.AddDays(timeframeValue);
                            reminder.InDays = timeframeValue;
                            break;
                        case TimeFrame.Weeks:
                            dateTime = dateTime.AddDays(timeframeValue * 7);
                            reminder.InWeeks = timeframeValue;
                            break;
                        case TimeFrame.Months:
                            dateTime = dateTime.AddMonths(timeframeValue);
                            reminder.InMonths = timeframeValue;
                            break;
                        case TimeFrame.Years:
                            dateTime = dateTime.AddYears(timeframeValue);
                            reminder.InYears = timeframeValue;
                            break;
                        default:
                            break;
                    }

                    index = 0;
                }
            }
            while (hasMatch);

            if (!hasTimeFrame)
            {
                dateTime = DateTime.MinValue;
            }
            reminder.DateTime = dateTime;
            return dateTime;
        }


        private bool IsRecurringReminder()
        {
            int index = 0;
            foreach (var word in completeInput)
            {
                if (word.Equals("every", StringComparison.OrdinalIgnoreCase))
                {
                    completeInput.RemoveAt(index);
                    return true;
                }
                index++;
            }
            return false;
        }

        private TimeOnly GetDefaultUtcTime(User owner)
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(owner.Timezone);
            var localDateTime = DateTime.Today.Add(defaultTime.ToTimeSpan());
            var userLocalDateTime = TimeZoneInfo.ConvertTime(localDateTime, timezone);
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(userLocalDateTime, timezone);
            var utcTimeOnly = TimeOnly.FromDateTime(utcDateTime);
            return utcTimeOnly;
        }

        private async Task SendMessageAsync(string message, ulong ownerId)
        {
            var ownerDiscord = await _client.Rest.GetUserAsync(ownerId);
            var ownerDm = await ownerDiscord.CreateDMChannelAsync();
            await ownerDm.SendMessageAsync(message);
        }

        private void CleanReminderMessage()
        {
            bool containsKeyword = true;

            if (completeInput.Count == 0)
            {
                return;
            }

            while (containsKeyword)
            {
                if (keywords.Any(keyword => completeInput[0].Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    completeInput.RemoveAt(0);
                }
                else
                {
                    containsKeyword = false;
                }
                if (completeInput.Count == 0)
                {
                    return;
                }
            }
        }

        private string RemoveDateSuffix(string input)
        {
            return Regex.Replace(input, @"(\d+)(st|nd|rd|th)", "$1");
        }
    }
}