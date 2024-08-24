using Discord.Interactions;

namespace MiraBot.Modules
{
    public class ReminderComponentHandler : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("select-menu")]
        public async Task SelectMenuHandler(string[] inputs)
        {
            MiramindersModule.result = int.Parse(inputs[0]);
        }
    }
}
