using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using MiraBot.DataAccess;

namespace MiraBot.Modules
{
    public class GroceryAssistantComponents : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService interactiveService;

        public GroceryAssistantComponents(InteractiveService interactiveService)
        {
            this.interactiveService = interactiveService;
        }


        public async Task GenerateMenuAsync<T>(
            List<T> items, 
            string placeholder, 
            string customId, 
            string? optionDescription,
            SocketInteractionContext context,
            string defaultOption = "Nevermind",
            string defaultOptionDescription = "Abandon this action")
        {
            var optionId = 0;
            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder(placeholder)
                .WithCustomId(customId)
                .AddOption(defaultOption, "nevermind", defaultOptionDescription);

            foreach (var item in items)
            {
                var name = item switch
                {
                    Meal meal => meal.Name,
                    string str => str
                };

                menuBuilder.AddOption(name, $"option-{optionId}", optionDescription);
                optionId++;
            }

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            var msg = await context.Channel.SendMessageAsync(components: builder.Build());

            var result = await interactiveService.NextInteractionAsync(x => x.User.Username == context.User.Username, timeout: TimeSpan.FromSeconds(120));

            if (result.IsSuccess)
            {
                await result.Value!.DeferAsync();
            }

            await msg.DeleteAsync();
        }


        public async Task GenerateModalAsync(string title, string customId, TextInputBuilder ti, TextInputBuilder ti2, SocketInteractionContext ctx)
        {
            var mb = new ModalBuilder()
                .WithTitle(title)
                .WithCustomId(customId)
                .AddTextInput(ti)
                .AddTextInput(ti2);
            await ctx.Interaction.RespondWithModalAsync(mb.Build());
        }
    }
}
