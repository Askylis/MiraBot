using Discord.Interactions;

namespace MiraBot.Modules
{
    public class ReminderComponentHandler : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("select-menu")]
        public void SelectMenuHandler(string[] inputs)
        {
            int parsedValue = int.Parse(inputs[0].Replace("option-", ""));
            MiramindersModule.result = parsedValue;
        }
    }
}
