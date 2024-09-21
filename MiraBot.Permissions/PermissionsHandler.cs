using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiraBot.Permissions
{
    public class PermissionsHandler
    {
        private readonly PermissionsRepository _repository;

        public PermissionsHandler(PermissionsRepository permissionsRepository)
        {
            _repository = permissionsRepository;
        }

        public async Task AddNewPermissionAsync(string name, string description)
        {
            var permission = new Permission { Name = name, Description = description};
            await _repository.AddNewPermissionAsync(permission);
        }

        public async Task<Permission> FindPermissionAsync(string name)
        {
            return await _repository.FindByNameAsync(name);
        }
    }
}