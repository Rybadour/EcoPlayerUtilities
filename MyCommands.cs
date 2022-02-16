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
using Eco.Gameplay.Civics;
using Eco.EM.Framework.ChatBase;

namespace EcoRebalanced
{
    public class MyCommands : IChatCommandHandler
    {
        [ChatCommand(
            "items",
            ChatAuthorizationLevel.User)
        ]
        public static void Items() { }

        [ChatSubCommand(
            "items",
            "List all items in your storage. Can filter by tag with the 'tagFilter' option. By default lists items in your backpack" +
            "but can be disabled with the 'includeBackpack' option.",
            "my-items",
            ChatAuthorizationLevel.User
        )]
        public static void List(User user, string tagFilter = "ALL", bool includeBackpack = true)
        {
            if (tagFilter == null) tagFilter = "ALL";

            Dictionary<int, int> itemTotals = Utils.getAllUserItems(user, tagFilter, includeBackpack);
            var sortedTotals = itemTotals.OrderBy(i => i, new ItemPairComparer());

            string listContents = "";
            foreach (var kv in sortedTotals)
            {
                Item item = Item.Get(kv.Key);
                listContents += $"{item.MarkedUpName} - {kv.Value}\n";
            }

            string title = "List of Your Items" + (tagFilter == "ALL" ? "" : " (" + tagFilter + ")");
            user.MsgLoc($"{title}");
            ChatBaseExtended.CBInfoPane(
                title,
                listContents,
                "list-items",
                user,
                true
            );
        }

        [ChatSubCommand("items", "List items needed by queued projects", "needed", ChatAuthorizationLevel.User)]
        public static void Needed(User user, User targetUser = null)
        {
            if (targetUser != null)
                user = targetUser;

            string listContents = "";
            Dictionary<int, int> itemsRemaining = Utils.getAllUserItems(user, "ALL", true);
            Dictionary<string, int> tagsRemaining = new Dictionary<string, int>();

            IEnumerable<WorkOrder> workOrders = Utils.getOwnedComponents<CraftingComponent>(user)
                .SelectMany(c => c.WorkOrders.AsEnumerable());
            foreach (WorkOrder wo in workOrders)
            {
                foreach (CraftingElement ce in wo.Recipe.Product)
                {
                    Utils.addToDictionaryValue(itemsRemaining, ce.Item.TypeID, (int)ce.Quantity.GetBaseValue * wo.UncraftedQuantity);
                }

                foreach (IStack stack in wo.IngredientsRemaningInAllIterations())
                {
                    int amountNeeded = stack.Quantity;
                    Tag tag = TagManager.Tag(stack.StackObject.Name);
                    if (tag != null)
                    {
                        Utils.subtractFromDictionaryValue(tagsRemaining, tag.Name, amountNeeded, false);
                    }
                    else
                    {
                        Item item = Item.Get(stack.StackObject.Name);
                        if (item != null)
                            Utils.subtractFromDictionaryValue(itemsRemaining, item.TypeID, amountNeeded, false);
                        else
                            user.MsgLoc($"{stack.StackObject.Name} is not an item?");
                    }
                }
            }

            foreach (var kv in tagsRemaining)
            {
                int amountNeeded = kv.Value * -1;
                Tag tag = TagManager.Tag(kv.Key);
                // Iterate over items in tag and subtract from items in order until amountNeeded is exhausted
                // or all items were iterated through.
                foreach (Type type in tag.TaggedTypes())
                {
                    Item item = Item.Get(type);
                    if (item != null)
                        amountNeeded = Utils.subtractFromDictionaryValue(itemsRemaining, item.TypeID, amountNeeded, true);

                    if (amountNeeded == 0) break;
                }

                tagsRemaining[kv.Key] = amountNeeded * -1;
            }

            var sorted = itemsRemaining.OrderBy(i => i, new ItemPairComparer());
            foreach (var kv in sorted)
            {
                Item item = Item.Get(kv.Key);
                if (kv.Value < 0)
                    listContents += $"{item.MarkedUpName} - {(kv.Value * -1)}\n";
            }
            foreach (var kv in tagsRemaining)
            {
                Tag tag = TagManager.Tag(kv.Key);
                if (kv.Value < 0)
                    listContents += $"{tag.MarkedUpName} - {(kv.Value * -1)}\n";
            }

            string title = "Your Projects are Missing These Items";
            user.MsgLoc($"{title}");
            ChatBase.Send(new ChatBase.InfoPane(
                title,
                listContents,
                "shopping-list",
                user,
                ChatBase.PanelType.InfoPanel,
                false,
                true
            ));
        }
    }
}
