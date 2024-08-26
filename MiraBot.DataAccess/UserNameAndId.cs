namespace MiraBot.DataAccess
{
    public class UserNameAndId
    {
        public UserNameAndId(string username, int id) 
        { 
            Username = username; 
            UserId = id; 
        }

        public string Username { get; set; }
        public int UserId { get; set; }
    }
}
