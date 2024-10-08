﻿using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MiraBot.Common;
using MiraBot.DataAccess;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MiraBot.Miraminders
{
    public partial class ReminderHandler
    {
        private readonly IRemindersCache _cache;
        private readonly TimeOnly defaultTime = new(17, 0);
        private List<string> completeInput;
        private static readonly string[] keywords = ["in", "on", "at", "every", "to", "that", "and", "from", "now", "a", "an", "next"];
        private readonly IOptions<MiraOptions> _options;
        private static readonly char[] separator = [' '];

        public ReminderHandler(
            IOptions<MiraOptions> options,
            IRemindersCache cache)
        {
            _options = options;
            _cache = cache;
        }

        public ReminderResult ParseReminderAsync(string input, User owner, User recipient)
        {
            // convert "my" and "me" in reminder message to "your" and "you"?
            // splits the input into a list for easier management
            completeInput = input.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (owner.Reminders.Count >= _options.Value.MaxReminderCount && owner.UserName != _options.Value.DevUserName)
            {
                return new ReminderResult
                {
                    IsSuccess = false,
                    Message = $"You've reached the maximum allowed number off reminders ({_options.Value.MaxReminderCount}). Please cancel a reminder before adding another.",
                    Reminder = null
                };
            }

            // makes new reminder that can be modified
            var reminder = new Reminder
            {
                OwnerId = owner.UserId,
                RecipientId = recipient.UserId
            };


            // search for keyword "every" to determine whether or not this reminder is recurring. If it's recurring, figure out
            // how often the reminder is supposed to go off. 
            reminder = CheckForRecurringReminder(reminder, owner);

            // extract the intended date and time from the reminder input
            if (!reminder.IsRecurring)
            {
                DateTime? dateTime = GetUserSpecifiedDateTimeAsync(owner, reminder);
                if (dateTime is null)
                {
                    return new ReminderResult
                    {
                        IsSuccess = false,
                        Message = "Couldn't find a valid date or time in your reminder input, so I can't build your reminder.",
                        Reminder = null
                    };
                }
                if (reminder.DateTime == DateTime.MinValue)
                {
                    reminder.DateTime = (DateTime)dateTime;
                }

                if (reminder.DateTime < DateTime.UtcNow)
                {
                    return new ReminderResult
                    {
                        IsSuccess = false,
                        Message = "Can't save your reminder because it's set to go off in the past.",
                        Reminder = null
                    };
                }
            }

            if (ReminderIsSpamAsync(reminder, owner))
            {
                return new ReminderResult
                {
                    IsSuccess = false,
                    Message = "To prevent unwanted spam, I can't send reminders to another user this frequently. Reminders to another user can't be within 5 minutes of each other.",
                    Reminder = null
                };
            }

            CleanReminderMessage();

            if (completeInput.Count == 0)
            {
                reminder.Message = "No attached message.";
            }
            else
            {
                reminder.Message = string.Join(" ", completeInput);
            }

            if (reminder.Message.Length > _options.Value.MaxMessageLength)
            {
                return new ReminderResult
                {
                    IsSuccess = false,
                    Message = $"I couldn't save your reminder because the attached message is too long. Max length for reminder messages is {_options.Value.MaxMessageLength} characters, but your message contained {reminder.Message.Length} characters.",
                    Reminder = null
                };
            }

            if (reminder.IsRecurring)
            {
                var frequency = GetRecurringReminderFrequency(reminder);
                return new ReminderResult
                {
                    IsSuccess = true, 
                    Message = $"Got it! Your reminder will go off every {frequency}. The next reminder will be sent on {DateOnly.FromDateTime(reminder.DateTime)} at {TimeOnly.FromDateTime(MiraminderService.ConvertUtcDateTimeToUser(reminder.DateTime, owner.Timezone))}.",
                    Reminder = reminder
                };
            }
            else
            {
                return new ReminderResult
                {
                    IsSuccess = true, 
                    Message = $"Got it! Your reminder will go off on {DateOnly.FromDateTime(reminder.DateTime)} at {TimeOnly.FromDateTime(MiraminderService.ConvertUtcDateTimeToUser(reminder.DateTime, owner.Timezone))}.",
                    Reminder = reminder
                };
            }
        }


        private DateTime? GetUserSpecifiedDateTimeAsync(User owner, Reminder reminder)
        {

            // try to get a date from the input, from either a date that's spelled out (August 17th), or from a 
            // numeric value (8/17)
            var date = GetDateFromString(owner) ?? GetNumericDateFormat(owner.UsesAmericanDateFormat.Value);

            // after attempting to get a date, regardless if the date is null or not, try to get a time.
            DateTime? dateTime = GetSpecifiedTimeAsync(owner);

            TimeOnly? time;
            // need to call GetDayOfWeek() somewhere in here. 
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
                var dayOfWeekResult = GetDayOfWeek(reminder, owner);
                if (dayOfWeekResult is null)
                {
                    var timeResult = GetTimeFromNow(reminder);
                    if (timeResult is null)
                    {
                        // if everything is null up to this point, then a valid date and/or time wasn't found within the input
                        return null;
                    }
                    else
                    {
                        return timeResult.DateTime;
                    }
                }
                else return dayOfWeekResult.DateTime;
            }

            else
            {
                time = GetDefaultUtcTime(owner);
            }

            if (date.HasValue && time.HasValue)
            {
                return date.Value.ToDateTime(time.Value);
            }

            return dateTime != DateTime.MinValue ? dateTime : null;
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
                var toRemove = new List<int> { index, removeIndex }.OrderByDescending(m => m);
                foreach (var element in toRemove)
                {
                    completeInput.RemoveAt(element);
                }
                return date;
            }

            return null;
        }

        private DateOnly? GetNumericDateFormat(bool isAmerican)
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
            DateTime dateTime = new();
            var index = 0;
            var timeIndex = -1;
            bool timeFound = false;
            List<int> removeRange = [];

            foreach (var word in completeInput)
            {
                if (DateTime.TryParse(word, out dateTime))
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
                    timeFound = true;
                    removeRange.Add(index);
                    break;
                }
                index++;
            }
            if (timeIndex > -1)
            {
                completeInput[timeIndex] = $"{completeInput[timeIndex]}{completeInput[timeIndex + 1]}";
                DateTime.TryParse(completeInput[timeIndex], out dateTime);
                UpdateCompleteInput(removeRange);
            }
            else
            {
                foreach (var element in removeRange)
                {
                    completeInput.RemoveAt(element);
                }
            }

            if (!timeFound)
            {
                return null;
            }

            var utcTime = MiraminderService.ConvertUserDateTimeToUtc(dateTime, owner.Timezone);
            if (utcTime < DateTime.UtcNow)
            {
                utcTime = utcTime.AddDays(1);
            }
            return utcTime;
        }

        private Reminder? GetTimeFromNow(Reminder reminder)
        {
            TimeFrame result = new();
            int timeframeValue = -1;
            bool hasMatch;
            bool hasTimeFrame = false;
            int index = 0;
            reminder.DateTime = DateTime.UtcNow;
            List<int> removeRange = [];
            do
            {
                hasMatch = false;
                foreach (var word in completeInput)
                {
                    var cleanedWord = RemoveCommaFromInput(word);
                    if (TimeframeMapping.timeFrameMapping.TryGetValue(cleanedWord, out result))
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
                    removeRange.Add(index);
                    removeRange.Add(index - 1);
                    if (index - 2 >= 0 && keywords.Contains(completeInput[index - 2]))
                    {
                        removeRange.Add(index - 2);
                    }
                }
                else if (hasMatch)
                {
                    timeframeValue = 1;
                    removeRange.Add(index);
                }

                if (hasMatch)
                {
                    switch (result)
                    {
                        case TimeFrame.Seconds:
                            reminder.DateTime = reminder.DateTime.AddSeconds(timeframeValue);
                            reminder.InSeconds = timeframeValue;
                            break;
                        case TimeFrame.Minutes:
                            reminder.DateTime = reminder.DateTime.AddMinutes(timeframeValue);
                            reminder.InMinutes = timeframeValue;
                            break;
                        case TimeFrame.Hours:
                            reminder.DateTime = reminder.DateTime.AddHours(timeframeValue);
                            reminder.InHours = timeframeValue;
                            break;
                        case TimeFrame.Days:
                            reminder.DateTime = reminder.DateTime.AddDays(timeframeValue);
                            reminder.InDays = timeframeValue;
                            break;
                        case TimeFrame.Weeks:
                            reminder.DateTime = reminder.DateTime.AddDays(timeframeValue * 7);
                            reminder.InWeeks = timeframeValue;
                            break;
                        case TimeFrame.Months:
                            reminder.DateTime = reminder.DateTime.AddMonths(timeframeValue);
                            reminder.InMonths = timeframeValue;
                            break;
                        case TimeFrame.Years:
                            reminder.DateTime = reminder.DateTime.AddYears(timeframeValue);
                            reminder.InYears = timeframeValue;
                            break;
                        default:
                            break;
                    }
                    UpdateCompleteInput(removeRange);
                    index = 0;
                    removeRange.Clear();
                }
            }
            while (hasMatch);

            if (!hasTimeFrame)
            {
                return null;
            }

            return reminder;
        }


        private Reminder CheckForRecurringReminder(Reminder reminder, User owner)
        {
            int index = 0;
            List<int> removeRange = [];
            foreach (var word in completeInput)
            {
                if (word.Equals("every", StringComparison.OrdinalIgnoreCase))
                {
                    removeRange.Add(index);
                    reminder.IsRecurring = true;
                    index = 0;
                    break;
                }
                index++;
            }


            if (reminder.IsRecurring)
            {
                completeInput.RemoveAt(index);
                var dayOfWeekResult = GetDayOfWeek(reminder, owner);
                if (dayOfWeekResult != null)
                {
                    reminder = dayOfWeekResult;
                }
                var timeFromNow = GetTimeFromNow(reminder);
                var dateResult = GetDateFromString(owner);
                var numericDateResult = GetNumericDateFormat(owner.UsesAmericanDateFormat.Value);
                var specifiedTime = GetSpecifiedTimeAsync(owner);

                if (timeFromNow is not null)
                {
                    reminder = timeFromNow;
                }

                else if (dateResult is not null)
                {
                    var time = GetDefaultUtcTime(owner);
                    reminder.DateTime = dateResult.Value.ToDateTime(time);
                    if (reminder.DateTime < DateTime.UtcNow)
                    {
                        reminder.DateTime = reminder.DateTime.AddYears(1);
                    }
                    reminder.InYears = 1;
                }

                else if (numericDateResult is not null)
                {
                    var time = GetDefaultUtcTime(owner);
                    reminder.DateTime = numericDateResult.Value.ToDateTime(time);
                    if (reminder.DateTime < DateTime.UtcNow)
                    {
                        reminder.DateTime = reminder.DateTime.AddYears(1);
                    }
                    reminder.InYears = 1;
                }

                // logic to update reminder with frequency at which reminder should be sent 
                // need to check for recurring syntax: "every 3rd Saturday" 
                // need to check for recurring syntax: "21st of every month" 
                // check for stuff like "every year on September 2nd that it's so-and-so's birthday"
                // if "every year" but no date, do a year from current date 
                // if "every <date>", make 1st DateTime for that date and then add a year
                // if "21st of every month"....well, that's self-explanatory. 
            }
            else
            {
                return reminder;
            }

            UpdateCompleteInput(removeRange);

            return reminder;
        }


        private Reminder? GetDayOfWeek(Reminder reminder, User owner)
        {
            DayOfWeek userDay = GetUserDayOfWeek(owner);
            DayOfWeek specifiedDay = new();
            int index = 0;
            List<int> removeRange = [];
            bool hasDayOfWeek = false;

            foreach (var word in completeInput)
            {
                if (Enum.TryParse(word, true, out specifiedDay) && Enum.IsDefined(typeof(DayOfWeek), specifiedDay))
                {
                    hasDayOfWeek = true;
                    removeRange.Add(index);
                    break;
                }
                index++;
            }

            if (hasDayOfWeek)
            {
                int daysFromNow = (int)specifiedDay - (int)userDay;
                reminder.DateTime = MiraminderService.ConvertUtcDateTimeToUser(DateTime.UtcNow, owner.Timezone).AddDays(daysFromNow);
                if (int.TryParse(completeInput[index - 1], out int timeFrameValue))
                {
                    if (reminder.DateTime < MiraminderService.ConvertUtcDateTimeToUser(DateTime.UtcNow, owner.Timezone))
                    {
                        reminder.DateTime = reminder.DateTime.AddDays(7);
                    }
                    reminder.InWeeks = timeFrameValue;
                    removeRange.Add(index - 1);
                }
                else if (completeInput[index - 1].Equals("next", StringComparison.OrdinalIgnoreCase))
                {
                    reminder.DateTime = reminder.DateTime.AddDays(7);
                    reminder.InWeeks = 1;
                    removeRange.Add(index - 1);
                }
                else if (completeInput[index - 1].Equals("other", StringComparison.OrdinalIgnoreCase))
                {
                    if (reminder.DateTime < MiraminderService.ConvertUtcDateTimeToUser(DateTime.UtcNow, owner.Timezone))
                    {
                        reminder.DateTime = reminder.DateTime.AddDays(7);
                    }
                    reminder.DateTime = reminder.DateTime.AddDays(14);
                    reminder.InWeeks = 2;
                    removeRange.Add(index - 1);
                }
                else
                {
                    if (reminder.DateTime < MiraminderService.ConvertUtcDateTimeToUser(DateTime.UtcNow, owner.Timezone))
                    {
                        reminder.DateTime = reminder.DateTime.AddDays(7);
                    }
                    reminder.InWeeks = 1;
                }

                return reminder;
            }
            else
            {
                return null;
            }
        }

        private TimeOnly GetDefaultUtcTime(User owner)
        {
            // this isn't working properly. It's converting to the wrong local time
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(owner.Timezone);
            var localDateTime = DateTime.Today.Add(defaultTime.ToTimeSpan());
            var userLocalDateTime = TimeZoneInfo.ConvertTime(localDateTime, timezone);
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(userLocalDateTime, timezone);
            var utcTimeOnly = TimeOnly.FromDateTime(utcDateTime);
            return utcTimeOnly;
        }

        private static DayOfWeek GetUserDayOfWeek(User owner)
        {
            TimeZoneInfo userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(owner.Timezone);
            DateTime utcNow = DateTime.UtcNow;
            DateTime userDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, userTimeZone);
            return userDateTime.DayOfWeek;
        }

        private bool ReminderIsSpamAsync(Reminder reminder, User owner)
        {
            var recipientReminders = _cache.GetCacheContentsByUser(reminder.RecipientId);

            bool isSpam = recipientReminders.Exists(r => r.OwnerId == reminder.OwnerId && r.RecipientId != reminder.OwnerId &&
            Math.Abs((r.DateTime - reminder.DateTime).TotalMinutes) <= 5);

            return (reminder.IsRecurring
                && reminder.OwnerId != reminder.RecipientId
                && reminder.DateTime.AddMinutes(5) <= DateTime.UtcNow.AddMinutes(5)
                && owner.UserName != _options.Value.DevUserName
                || reminder.OwnerId != reminder.RecipientId
                && owner.UserName != _options.Value.DevUserName
                && isSpam);
        }


        private static string RemoveCommaFromInput(string input)
        {
            if (input.EndsWith(','))
            {
                return input.TrimEnd(',');
            }
            else return input;
        }

        public void UpdateCompleteInput(List<int> removeRange)
        {
            removeRange = removeRange.OrderByDescending(x => x).ToList();
            foreach (var element in removeRange)
            {
                completeInput.RemoveAt(element);
            }
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
                if (Array.Exists(keywords, keyword => string.Equals(completeInput[0], keyword, StringComparison.OrdinalIgnoreCase)))
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

        private static string RemoveDateSuffix(string input)
        {
            return MyRegex().Replace(input, "$1");
        }


        private static string GetRecurringReminderFrequency(Reminder reminder)
        {
            List<string> frequencyList = [];

            if (reminder.InYears > 0)
                frequencyList.Add($"{reminder.InYears} year{(reminder.InYears > 1 ? "s" : "")}");
            if (reminder.InMonths > 0)
                frequencyList.Add($"{reminder.InMonths} month{(reminder.InMonths > 1 ? "s" : "")}");
            if (reminder.InWeeks > 0)
                frequencyList.Add($"{reminder.InWeeks} week{(reminder.InWeeks > 1 ? "s" : "")}");
            if (reminder.InDays > 0)
                frequencyList.Add($"{reminder.InDays} day{(reminder.InDays > 1 ? "s" : "")}");
            if (reminder.InHours > 0)
                frequencyList.Add($"{reminder.InHours} hour{(reminder.InHours > 1 ? "s" : "")}");
            if (reminder.InMinutes > 0)
                frequencyList.Add($"{reminder.InMinutes} minute{(reminder.InMinutes > 1 ? "s" : "")}");
            if (reminder.InSeconds > 0)
                frequencyList.Add($"{reminder.InSeconds} second{(reminder.InSeconds > 1 ? "s" : "")}");

            string frequency = frequencyList.Count > 0 ? string.Join(", ", frequencyList) : "once";

            return frequency;
        }

        [GeneratedRegex(@"(\d+)(st|nd|rd|th)")]
        private static partial Regex MyRegex();
    }
}