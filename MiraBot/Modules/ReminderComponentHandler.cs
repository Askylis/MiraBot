﻿using Discord.Interactions;
using MiraBot.Common;

namespace MiraBot.Modules
{
    public class ReminderComponentHandler : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("select-menu")]
        public static async Task SelectMenuHandler(string[] inputs)
        {
            if (inputs[0] == "nevermind")
            {
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
