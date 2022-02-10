using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Property;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.EM.Framework.ChatBase;

namespace EcoRebalanced
{
    public class MyCommands : IChatCommandHandler
    {
        [ChatCommand("List all items in your storage. Can filter by tag with 'tagFilter'.", "my-items", ChatAuthorizationLevel.User)]
        public static void MyItems(User user, string tagFilter = "ALL")
        {
            if (tagFilter == null) tagFilter = "ALL";

            SortedDictionary<string, int> itemTotals = Utils.getAllUserItems(user, tagFilter);

            string listContents = "";
            foreach (var item in itemTotals)
            {
                listContents += $"{item.Key} - {item.Value}\n";
            }
            ChatBaseExtended.CBInfoPane(
                "List of Your Items" + (tagFilter == "ALL" ? "" : " (" + tagFilter + ")"),
                listContents,
                "list-items"
            );
        }

        [ChatCommand("List items needed by current projects", "my-needed-items", ChatAuthorizationLevel.User)]
        public static void MyNeededItems(User user)
        {
            string listContents = "";
            SortedDictionary<string, int> itemsRemaining = Utils.getAllUserItems(user, "ALL");

            foreach (CraftingComponent table in Utils.getOwnedComponents<CraftingComponent>(user))
            {
                foreach (WorkOrder order in table.WorkOrders.AsEnumerable())
                {
                    foreach (IStack stack in order.IngredientsRemaningInAllIterations())
                    {
                        int amountNeeded = stack.Quantity;
                        Tag tag = TagManager.Tag(stack.StackObject.Name);
                        if (tag != null)
                        {
                            // Iterate over items in tag and subtract from items in order until amountNeeded is exhausted
                            // or all items were iterated through.
                            foreach (Item item in TagExtensions.TaggedItems(tag))
                            {
                                amountNeeded = Utils.subtractFromItem(itemsRemaining, item.DisplayName, amountNeeded, true);
                                if (amountNeeded == 0) break;
                            }

                            if (amountNeeded > 0)
                            {
                                Utils.subtractFromItem(itemsRemaining, tag.DisplayName, amountNeeded, false);
                            }
                        }
                        else
                        {
                            Utils.subtractFromItem(itemsRemaining, stack.StackObject.DisplayName, amountNeeded, false);
                        }
                    }
                }
            }

            foreach (var item in itemsRemaining)
            {
                if (item.Value < 0)
                    listContents += $"<ecoicon item='{item.Key}'></ecoicon>{item.Key} - {item.Value * -1}\n";
            }

            ChatBaseExtended.CBInfoBox(
                "Your Projects are Missing These Items\n" +
                listContents
            );
        }
    }
}
