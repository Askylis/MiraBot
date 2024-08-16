using FluentAssertions;
using Microsoft.Extensions.Logging;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using Moq;

namespace MiraBot.Miraminders.UnitTests
{
    public class Tests
    {
        private ILogger<MiraminderService> _logger;
        private Mock<IMiramindersRepository> _repository;
        private readonly ulong _discordId = 1;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger<MiraminderService>>().Object;
            _repository = new Mock<IMiramindersRepository>();
        }

        [Test]
        // What you're testing_condition you're testing_what you expect
        public async Task GetUserTimezone_UserHasTimezone_ReturnsTimezone()
        {
            // Arrange
            string timezone = "test";
            _repository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(new User { DiscordId = _discordId, Timezone = timezone });
            var service = new MiraminderService(_repository.Object, _logger);

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
            var service = new MiraminderService(_repository.Object, _logger);

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
            var service = new MiraminderService(_repository.Object, _logger);

            // Act
            var result = await service.EnsureUserExistsAsync(_discordId, username);

            // Assert
            Assert.That(result, Is.EqualTo(user));
        }

        [Test]
        public async Task EnsureUserExists_UserDoesNotExist_AddsNewUser()
        {
            // Arrange
            var service = new MiraminderService(_repository.Object, _logger);
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
            var service = new MiraminderService(_repository.Object, _logger);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var user = new User { DiscordId = _discordId, UserId = 2 };
            _repository.Setup(r => r.GetUserByDiscordIdAsync(_discordId)).ReturnsAsync(user);

            // Act
            await service.AddReminderAsync(_discordId, _discordId, message, date, false);

            // Assert
            _repository.Verify(r => r.AddReminderAsync(It.Is<Reminder>(n => n.OwnerId == user.UserId && n.RecipientId == user.UserId && n.Message == message)), Times.Once());
        }

        [Test]
        public async Task AddReminder_OwnerIsNull_ExceptionIsThrown()
        {
            // Arrange 
            var service = new MiraminderService(_repository.Object, _logger);
            var message = "test";
            var date = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);

            // Act
            Func<Task> action = async () => await service.AddReminderAsync(_discordId, _discordId, message, date, false);


            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
            //Assert.ThrowsAsync<InvalidOperationException>(async () => await service.AddReminderAsync(_discordId, _discordId, message, date, false));
        }
    }
}