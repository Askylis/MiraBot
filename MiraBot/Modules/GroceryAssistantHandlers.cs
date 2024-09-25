using Discord.Interactions;
using MiraBot.Common;

namespace MiraBot.Modules
{
    public class GroceryAssistantHandlers : InteractionModuleBase<SocketInteractionContext>
    {
        //these are kinda redundant now bc all the menus do the same thing. But I'm gonna leave what I have here anyway.
        //In case I ever change how the menus work, I already have all this setup and ready to go so it'll be easier to modify. 
        [ComponentInteraction("override-menu")]
        public async Task OverrideMenuHandler(string[] inputs)
        {
            await MenuHandler(inputs, "override");
        }


        [ComponentInteraction("delete-menu")]
        public async Task DeleteMenuHandler(string[] inputs)
        {
            await MenuHandler(inputs, "delete");
        }

        [ComponentInteraction("edit-menu")]
        public async Task EditMenuHandler(string[] inputs)
        {
            await MenuHandler(inputs, "edit");
        }

        [ComponentInteraction("recipe-menu")]
        public async Task RecipeMenuHandler(string[] inputs)
        {
            await MenuHandler(inputs, "recipe");
        }

        [ComponentInteraction("share-menu")]
        public async Task ShareMenuHandler(string[] inputs)
        {
            await MenuHandler(inputs, "share");
        }

        public async Task MenuHandler(string[] inputs, string menuType)
        {
            if (inputs[0] == "nevermind")
            {
                string message = menuType switch
                {
                    "override" => "You chose not to override anything.",
                    "delete" => "All right, no problem!",
                    "edit" => "All right, no problem!",
                    "recipe" => "All right, no problem!",
                    "share" => "All right, no problem!"
                };

                await ReplyAsync(message);
                ModuleHelpers.result = -1;

            }
            else
            {
                int parsedValue = int.Parse(inputs[0].Replace("option-", ""));
                ModuleHelpers.result = parsedValue;
            }
        }

    }
}
