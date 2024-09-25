using Microsoft.Extensions.Logging;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Miraminders
{
    public class UsersCache : IUsersCache
    {
        private static List<UserNameAndId> _cache = new();

        private readonly IUsersRepository _repository;
        private readonly ILogger<UsersCache> _logger;

        public UsersCache(ILogger<UsersCache> logger, IUsersRepository usersRepository)
        {
            _logger = logger;
            _repository = usersRepository;
        }


        public async Task RefreshCacheAsync()
        {
            _logger.LogInformation("Refreshing users cache...");
            _cache.Clear();
            var users = await _repository.GetUserNamesAndIdsAsync();
            _logger.LogDebug("Number of users: {usersCount}", users.Count);
            _cache.AddRange(users);
        }

        public UserNameAndId? GetUserByName(string input)
        {
            return _cache.Find(u => u.Username.Equals(input, StringComparison.OrdinalIgnoreCase));
        }
    }
}
