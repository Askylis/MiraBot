using MiraBot.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiraBot.Miraminders
{
    public interface IUsersCache
    {
        Task RefreshCacheAsync();
        UserNameAndId? GetUserByName(string input);
    }
}
