using Discord.Interactions;

namespace MiraBot.Modules
{
    public class GroceryAssistantHandlers : InteractionModuleBase<SocketInteractionContext>
    {
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

        public async Task MenuHandler(string[] inputs, string menuType)
        {
            if (inputs[0] == "nevermind")
            {
                string message = menuType switch
                {
                    "override" => "You chose not to override anything.",
                    "delete" => "All right, no problem!",
                    "edit" => "All right, no problem!",
                };

                await ReplyAsync(message);

                if (menuType == "override")
                    // I KNOW THAT GETTING THE VALUES FROM THESE MENU HANDLERS IN THIS WAY IS WRONG AND VERY UGLY
                    // PLEASE DON'T YELL AT ME
                    // I don't know a better way to do it yet and was gonna ask for your help :(
                    // I tried tons of different solutions and just couldn't get anything else to work so I'm kinda stumped
                    GroceryAssistantModule.overrideValue = -1;
                else 
                    GroceryAssistantModule.modifyValue = -1;

            }
            else
            {
                int parsedValue = int.Parse(inputs[0].Replace("option-", ""));

                if (menuType == "override")
                    GroceryAssistantModule.overrideValue = parsedValue;
                else 
                    GroceryAssistantModule.modifyValue = parsedValue;
            }
        }

    }
}
