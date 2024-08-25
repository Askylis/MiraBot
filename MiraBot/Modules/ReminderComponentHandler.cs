using Discord.Interactions;

namespace MiraBot.Modules
{
    public class ReminderComponentHandler : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("select-menu")]
        public async Task SelectMenuHandler(string[] inputs)
        {
            if (inputs[0] == "nevermind")
            {
                MiramindersModule.result = -1;
                return;
            }

            else
            {
                int parsedValue = int.Parse(inputs[0].Replace("option-", ""));
                MiramindersModule.result = parsedValue;
            }
        }
    }
}
