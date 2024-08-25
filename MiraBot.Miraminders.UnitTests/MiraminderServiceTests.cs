using Discord;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MiraBot.Common;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using Moq;
using System.Globalization;

namespace MiraBot.Miraminders.UnitTests
{
    public class Tests
    {
        private ILogger<MiraminderService> _logger;
        private Mock<IMiramindersRepository> _repository;
        private Mock<IDateTimeProvider> _dateTimeProvider;
        private readonly ulong _discordId = 1;
        private readonly int _userId = 5;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger<MiraminderService>>().Object;
            _repository = new Mock<IMiramindersRepository>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
        }

        [Test]
        // What you're testing_condition you're testing_what you expect
        public async Task GetUserTimezone_UserHasTimezone_ReturnsTimezone()
        {
            // Arrange
            string timezone = "test";
            _repository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(new User { DiscordId = _discordId, Timezone = timezone });
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);

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
            _repository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(new User { DiscordId = _discordId, Timezone = timezone });
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);

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
            _repository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(user);
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);

            // Act
            var result = await service.EnsureUserExistsAsync(_discordId, username);

            // Assert
            Assert.That(result, Is.EqualTo(user));
        }

        [Test]
        public async Task EnsureUserExists_UserDoesNotExist_AddsNewUser()
        {
            // Arrange
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);
            var username = "test";

            // Act
            var result = await service.EnsureUserExistsAsync(_discordId, username);

            // Assert 
            _repository.Verify(r => r.AddNewUserAsync(It.Is<User>(u => u.UserName == username)), Times.Once());
            Assert.That(result.UserName, Is.EqualTo(username));
        }

        [Test]
        public async Task AddReminder_OwnerAndRecipientAreSame_ReminderIsAdded()
        {
            // Arrange
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var user = new User { DiscordId = _discordId, UserId = 2 };
            _repository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(user);
            var reminder = new Reminder { OwnerId = _userId, RecipientId = _userId, Message = message, DateTime = date };

            // Act
            await service.AddReminderAsync(reminder);

            // Assert
            _repository.Verify(r => r.AddReminderAsync(It.Is<Reminder>(n => n.OwnerId == user.UserId && n.RecipientId == user.UserId && n.Message == message)), Times.Once());
        }

        [Test]
        public async Task AddReminder_OwnerIsNull_ExceptionIsThrown()
        {
            // Arrange 
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);
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
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var user = new User { DiscordId = _discordId, UserId = 2 };
            var recipient = new User { DiscordId = 2, UserId = 3 };
            int recipientId = 2;
            var reminder = new Reminder { OwnerId = _userId, RecipientId = recipientId, Message = message, DateTime = date };

            // Act
            _repository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(user);
            _repository.Setup(r => r.GetUserByDiscordIdAsync(recipient.DiscordId)).ReturnsAsync(recipient);
            await service.AddReminderAsync(reminder);

            // Assert 
            _repository.Verify(r => r.AddReminderAsync(It.Is<Reminder>(n => n.OwnerId == user.UserId && n.RecipientId == recipient.UserId && n.Message == message)), Times.Once());
        }

        [Test]
        public async Task UpdateRecurringReminder_DateTimeIsDifferent_ReminderIsUpdated()
        {
            // Arrange
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);
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
                DateTime = originalDateTime

            };

            // Act
            await service.UpdateRecurringReminderAsync(reminder);

            // Assert
            _repository.Verify(r => r.UpdateReminderAsync(It.Is<Reminder>(r => r.DateTime == originalDateTime.AddDays(1))), Times.Once());
            Assert.That(reminder.DateTime, Is.EqualTo(originalDateTime.AddDays(1)));
        }

        [Test]
        [TestCase("19:00:00", "US Eastern Standard Time", "2024-08-17T23:00:00Z")]
        [TestCase("19:00:00", "GMT Standard Time", "2024-08-17T18:00:00Z")]
        public void ConvertUserTimeToUtc_TimezoneIsValid_ReturnsUtcTime(string userTime, string timezone, string expected)
        {
            // Arrange
            var service = new MiraminderService(_repository.Object, _logger, _dateTimeProvider.Object);
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