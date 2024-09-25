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
        private Mock<IUsersCache> _usersCache;
        private readonly ulong _discordId = 1;
        private readonly int _userId = 5;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger<MiraminderService>>().Object;
            _remindersRepository = new Mock<IMiramindersRepository>();
            _usersRepository = new Mock<IUsersRepository>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
            _usersCache = new Mock<IUsersCache>();
        }

        [Test]
        // What you're testing_condition you're testing_what you expect
        public async Task GetUserTimezone_UserHasTimezone_ReturnsTimezone()
        {
            // Arrange
            string timezone = "test";
            _usersRepository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(new User { DiscordId = _discordId, Timezone = timezone });
            var service = new MiraminderService(_remindersRepository.Object, _logger, _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);

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
            var service = new MiraminderService(_remindersRepository.Object, _logger, _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);

            // Act
            var result = await service.GetUserTimeZoneAsync(_discordId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task EnsureUserExists_UserExists_ReturnsUser()
        {
            // Arrange
            var username = "test";
            var user = new User { DiscordId = _discordId };
            _usersRepository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(user);
            var service = new MiraminderService(_remindersRepository.Object, _logger, _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);

            // Act
            //var result = await service.EnsureUserExistsAsync(_discordId, username);

            // Assert
            //Assert.That(result, Is.EqualTo(user));
        }

        [Test]
        public async Task EnsureUserExists_UserDoesNotExist_AddsNewUser()
        {
            // Arrange
            var service = new MiraminderService(_remindersRepository.Object, _logger, _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);
            var username = "test";

            // Act
            //var result = await service.EnsureUserExistsAsync(_discordId, username);

            // Assert 
            _usersRepository.Verify(r => r.AddNewUserAsync(It.Is<User>(u => u.UserName == username)), Times.Once());
            //Assert.That(result.UserName, Is.EqualTo(username));
        }

        [Test]
        public async Task AddReminder_OwnerAndRecipientAreSame_ReminderIsAdded()
        {
            // Arrange
            var service = new MiraminderService(_remindersRepository.Object, _logger, _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var user = new User { DiscordId = _discordId, UserId = 2 };
            _usersRepository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(user);
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
            var service = new MiraminderService(_remindersRepository.Object, _logger, _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var reminder = new Reminder { OwnerId = _userId, RecipientId = _userId, Message = message, DateTime = date };

            // Act
            Func<Task> action = async () => await service.AddReminderAsync(reminder);


            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
            //Assert.ThrowsAsync<InvalidOperationException>(async () => await service.AddReminderAsync(_discordId, _discordId, message, date, false));
        }

        [Test]
        public async Task AddReminder_OwnerAndRecipientAreDifferent_ReminderIsAdded()
        {
            // Arrange
            var service = new MiraminderService(_remindersRepository.Object, _logger, _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var user = new User { DiscordId = _discordId, UserId = 2 };
            var recipient = new User { DiscordId = 2, UserId = 3 };
            int recipientId = 2;
            var reminder = new Reminder { OwnerId = _userId, RecipientId = recipientId, Message = message, DateTime = date };

            // Act
            _usersRepository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(user);
            _usersRepository.Setup(r => r.GetUserByDiscordIdAsync(recipient.DiscordId)).ReturnsAsync(recipient);
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
                _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);
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
            Assert.That(reminder.DateTime, Is.EqualTo(expectedDateTime));
            Assert.False(reminder.IsCompleted);
        }

        [Test]
        [TestCase("19:00:00", "US Eastern Standard Time", "2024-08-17T23:00:00Z")]
        [TestCase("19:00:00", "GMT Standard Time", "2024-08-17T18:00:00Z")]
        public void ConvertUserTimeToUtc_TimezoneIsValid_ReturnsUtcTime(string userTime, string timezone, string expected)
        {
            // Arrange
            var service = new MiraminderService(_remindersRepository.Object, _logger, _dateTimeProvider.Object, _usersCache.Object, _usersRepository.Object);
            _dateTimeProvider.SetupGet(d => d.Today).Returns(new DateTime(2024, 8, 17, 0, 0, 0, DateTimeKind.Local));

            // Act
            var result = service.ConvertUserTimeToUtc(TimeOnly.ParseExact(userTime, "HH:mm:ss", CultureInfo.InvariantCulture), timezone);

            // Assert
            Assert.That(result, Is.EqualTo(DateTime.Parse(expected, null, DateTimeStyles.RoundtripKind)));
        }

        [Test]
        public void ConvertUtcToUserTime_TimezoneIsValid_ReturnsUserTime()
        {
            // Arrange

            // Act

            // Assert
        }
    }
}