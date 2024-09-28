using FluentAssertions;
using Microsoft.Extensions.Logging;
using MiraBot.Common;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using Moq;
using System.Globalization;

namespace MiraBot.Miraminders.UnitTests
{
    public class MiraminderServiceTests
    {
        private ILogger<MiraminderService> _logger;
        private Mock<IMiramindersRepository> _remindersRepository;
        private Mock<IUsersRepository> _usersRepository;
        private Mock<IDateTimeProvider> _dateTimeProvider;
        private readonly ulong _discordId = 1;
        private readonly int _userId = 5;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger<MiraminderService>>().Object;
            _remindersRepository = new Mock<IMiramindersRepository>();
            _usersRepository = new Mock<IUsersRepository>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
        }

        [Test]
        // What you're testing_condition you're testing_what you expect
        public async Task GetUserTimezone_UserHasTimezone_ReturnsTimezone()
        {
            // Arrange
            string timezone = "test";
            _usersRepository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(new User { DiscordId = _discordId, Timezone = timezone });
            var service = new MiraminderService(_remindersRepository.Object, _logger, _usersRepository.Object);

            // Act
            var result = await service.GetUserTimeZoneAsync(_discordId);

            // Assert
            Assert.That(result, Is.EqualTo(timezone));
        }

        [Test]
        // What you're testing_condition you're testing_what you expect
        public async Task GetUserTimezone_TimezoneIsNull_DoesNotThrow()
        {
            // Arrange
            string? timezone = null;
            _usersRepository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(new User { DiscordId = _discordId, Timezone = timezone });
            var service = new MiraminderService(_remindersRepository.Object, _logger, _usersRepository.Object);

            // Act
            var result = await service.GetUserTimeZoneAsync(_discordId);

            // Assert
            Assert.That(result, Is.Null);
        }


        [Test]
        public async Task AddReminder_OwnerAndRecipientAreSame_ReminderIsAdded()
        {
            // Arrange
            var service = new MiraminderService(_remindersRepository.Object, _logger, _usersRepository.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var user = new User { DiscordId = _discordId, UserId = _userId, UserName = "test" };
            _usersRepository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(user);
            _usersRepository.Setup(r => r.GetUserByUserIdAsync(_userId)).ReturnsAsync(user);
            var reminder = new Reminder { OwnerId = _userId, RecipientId = _userId, Message = message, DateTime = date };

            // Act
            await service.AddReminderAsync(reminder);

            // Assert
            _remindersRepository.Verify(r => r.AddReminderAsync(It.Is<Reminder>(n => n.OwnerId == user.UserId && n.RecipientId == user.UserId && n.Message == message)), Times.Once());
        }

        [Test]
        public async Task AddReminder_OwnerIsNull_ExceptionIsThrown()
        {
            // Arrange 
            var service = new MiraminderService(_remindersRepository.Object, _logger, _usersRepository.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var reminder = new Reminder { OwnerId = _userId, RecipientId = _userId, Message = message, DateTime = date };

            // Act
            Func<Task> action = async () => await service.AddReminderAsync(reminder);


            // Assert
            await action.Should().ThrowAsync<NullReferenceException>();
            Assert.ThrowsAsync<NullReferenceException>(async () => await service.AddReminderAsync(reminder));
        }

        [Test]
        public async Task AddReminder_OwnerAndRecipientAreDifferent_ReminderIsAdded()
        {
            // Arrange
            var service = new MiraminderService(_remindersRepository.Object, _logger, _usersRepository.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var user = new User { DiscordId = _discordId, UserId = 2, UserName = "test" };
            var recipient = new User { DiscordId = 2, UserId = 3, UserName = "recipient" };
            var reminder = new Reminder { OwnerId = user.UserId, RecipientId = recipient.UserId, Message = message, DateTime = date };

            // Act
            _usersRepository.Setup(r => r.GetUserByUserIdAsync(user.UserId)).ReturnsAsync(user);
            await service.AddReminderAsync(reminder);

            // Assert 
            _remindersRepository.Verify(r => r.AddReminderAsync(It.Is<Reminder>(n => n.OwnerId == user.UserId && n.RecipientId == recipient.UserId && n.Message == message)), Times.Once());
        }

        [Test]
        public async Task UpdateRecurringReminder_DateTimeIsUpdated_Correctly()
        {
            // Arrange
            var service = new MiraminderService(
                _remindersRepository.Object, _logger,
                _usersRepository.Object);
            var originalDateTime = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var ownerId = 1;
            var message = "test";
            var reminder = new Reminder
            {
                OwnerId = ownerId,
                RecipientId = ownerId,
                IsRecurring = false,
                IsCompleted = false,
                Message = message,
                DateTime = originalDateTime,
                InSeconds = 1,
                InMinutes = 1,
                InHours = 1,
                InDays = 1,
                InWeeks = 1,
                InMonths = 1,
                InYears = 1,
            };

            var expectedDateTime = originalDateTime
                .AddSeconds(1)
                .AddMinutes(1)
                .AddHours(1)
                .AddDays(1)
                .AddDays(7)
                .AddMonths(1)
                .AddYears(1);

            // Act
            await service.UpdateRecurringReminderAsync(reminder);

            // Assert
            _remindersRepository.Verify(r => r.UpdateReminderAsync(It.Is<Reminder>(r => r.DateTime == expectedDateTime)), Times.Once());
            Assert.Multiple(() =>
            {
                Assert.That(reminder.DateTime, Is.EqualTo(expectedDateTime));
                Assert.That(reminder.IsCompleted, Is.False);
            });
        }

        [Test]
        [TestCase("19:00:00", "US Eastern Standard Time", "2024-08-17T23:00:00Z")]
        [TestCase("19:00:00", "GMT Standard Time", "2024-08-17T18:00:00Z")]
        public void ConvertUserDateTimeToUtc_TimezoneIsValid_ReturnsUtcTime(string userTime, string timezone, string expected)
        {
            // Arrange
            var utcNow = new DateTime(2024, 8, 17, 0, 0, 0, DateTimeKind.Utc);
            _dateTimeProvider.SetupGet(d => d.UtcNow).Returns(utcNow);

            var requestedDateTime = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day,
                int.Parse(userTime.Split(':')[0]),
                int.Parse(userTime.Split(':')[1]),
                int.Parse(userTime.Split(':')[2]),
                DateTimeKind.Unspecified);

            // Act
            var result = MiraminderService.ConvertUserDateTimeToUtc(requestedDateTime, timezone);

            // Assert
            Assert.That(result, Is.EqualTo(DateTime.Parse(expected, null, DateTimeStyles.RoundtripKind)));
        }


        [Test]
        [TestCase("2024-08-17T23:00:00Z", "US Eastern Standard Time", "19:00:00")]
        [TestCase("2024-08-17T18:00:00Z", "GMT Standard Time", "19:00:00")]
        public void ConvertUtcToUserTime_TimezoneIsValid_ReturnsUserTime(string utcTime, string timezone, string expectedUserTime)
        {
            // Arrange
            var utcDateTime = DateTime.Parse(utcTime, null, DateTimeStyles.RoundtripKind);

            // Act
            var result = MiraminderService.ConvertUtcDateTimeToUser(utcDateTime, timezone);

            // Assert
            Assert.That(result.ToString("HH:mm:ss"), Is.EqualTo(expectedUserTime));
        }

        [Test]
        public async Task CancelReminder_ReminderCancelled_Successfully()
        {
            // Arrange
            var service = new MiraminderService(_remindersRepository.Object, _logger, _usersRepository.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var user = new User { DiscordId = _discordId, UserId = 2, UserName = "test" };
            var recipient = new User { DiscordId = 2, UserId = 3, UserName = "recipient" };
            var reminder = new Reminder { OwnerId = user.UserId, RecipientId = recipient.UserId, Message = message, DateTime = date, ReminderId = 1, IsRecurring = true };

            _remindersRepository.Setup(r => r.RemoveReminderAsync(reminder.ReminderId))
                .Callback(() =>
                {
                    reminder.IsCompleted = true;
                })
                .Returns(Task.CompletedTask);

            // Act
            await service.CancelReminderAsync(reminder);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(reminder.IsRecurring, Is.False);
                Assert.That(reminder.IsCompleted, Is.True);
            });
            _remindersRepository.Verify(r => r.RemoveReminderAsync(reminder.ReminderId), Times.Once);
        }
    }
}