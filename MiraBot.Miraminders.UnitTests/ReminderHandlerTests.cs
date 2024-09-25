using FluentAssertions;
using Microsoft.Extensions.Logging;
using MiraBot.Common;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using Moq;
using System.Globalization;

namespace MiraBot.Miraminders.UnitTests
{
    public class ReminderHandlerTests
    {
        private ILogger<MiraminderService> _logger;
        private Mock<IMiramindersRepository> _remindersRepository;
        private Mock<UsersRepository> _usersRepository;
        private Mock<IDateTimeProvider> _dateTimeProvider;
        private Mock<UsersCache> _usersCache;
        private readonly ulong _discordId = 1;
        private readonly int _userId = 5;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger<MiraminderService>>().Object;
            _remindersRepository = new Mock<IMiramindersRepository>();
            _usersRepository = new Mock<UsersRepository>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
        }
    }
}
