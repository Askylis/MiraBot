using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiraBot.Miraminders
{
    public class Miraminder
    {
        private readonly MiramindersRepository _repository;
        public Miraminder(MiramindersRepository repository)
        {
            _repository = repository;
        }


        public async Task<string> UserTimeZone(ulong discordId)
        {
            return await _repository.GetUserTimeZone(discordId);
        }

        public async Task<User> GetUserAsync(int userId)
        {
            return await _repository.GetUserByUserId(userId);
        }

        public async Task<bool> UserExistsAsync(ulong discordId)
        {
            return await _repository.UserExistsAsync(discordId);
        }

        public async Task AddNewUserAsync(string userName, ulong discordId)
        {
            var user = new User
            {
                UserName = userName,
                DiscordId = discordId
            };
            await _repository.AddNewUserAsync(user);
        }

        public async Task<User> GetUserByNameAsync(string userName)
        {
            return await _repository.GetUserByNameAsync(userName);
        }

        public async Task<List<Reminder>> GetRemindersByUserAsync(ulong discordId)
        {
            var user = await _repository.GetUserByDiscordIdAsync(discordId);
            var reminders = await _repository.GetRemindersAsync();
            return reminders.Where(r => r.OwnerId == user.UserId).ToList();
        }

        public static async Task DeleteReminderAsync()
        {

        }

        public void BuildTimezoneFile(string filePath)
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            using (StreamWriter writer = new(filePath))
            {
                foreach (var timeZone in timeZones)
                {
                    writer.Write($"{timeZone}\n");
                }
            }
        }


        public string GetOutputPath(string fileName)
        {
            string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fullFilePath = Path.Combine(myDocumentsPath, fileName);
            return fullFilePath;
        }
    }
}
