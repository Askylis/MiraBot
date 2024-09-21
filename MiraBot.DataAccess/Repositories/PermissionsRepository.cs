using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiraBot.DataAccess.Repositories
{
    public class PermissionsRepository
    {
        private readonly DatabaseOptions _databaseOptions;

        public PermissionsRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            _databaseOptions = databaseOptions.Value;
        }

        public async Task UserHasPermission(int userId, string permissionName)
        {

        }

        public async Task UpdateUserPermissionsAsync()
        {

        }

        public async Task AddNewPermission()
        {

        }

        public async Task DeletePermissionAsync()
        {

        }
    }
}