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

            Dictionary<Item, int> itemTotals = Utils.getAllUserItems(user, tagFilter);
            var sortedTotals = itemTotals.OrderBy(i => i, new ItemPairComparer());

            string listContents = "";
            foreach (var kv in sortedTotals)
            {
                listContents += kv.Key.UILinkAndNumber(kv.Value) + "\n";
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
            Dictionary<Item, int> itemsRemaining = Utils.getAllUserItems(user, "ALL");
            Dictionary<Tag, int> tagsRemaining = new Dictionary<Tag, int>();

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
                            foreach (Type type in tag.TaggedTypes())
                            {
                                Item item = Item.Get(type);
                                //user.MsgLoc($"{(item == null ? "Cannot find tagged item for " + type.FullName : "Found item " + item.DisplayName)}\n");
                                if (item != null)
                                    amountNeeded = Utils.subtractFromDictionaryValue(itemsRemaining, item, amountNeeded, true);

                                if (amountNeeded == 0) break;
                            }

                            if (amountNeeded > 0)
                            {
                                Utils.subtractFromDictionaryValue(tagsRemaining, tag, amountNeeded, false);
                            }
                        }
                        else
                        {
                            Item item = Item.Get(stack.StackObject.Name);
                            if (item != null)
                                Utils.subtractFromDictionaryValue(itemsRemaining, item, amountNeeded, false);
                            else
                                user.MsgLoc($"{stack.StackObject.Name} is not an item?");
                        }
                    }
                }
            }

            var sorted = itemsRemaining.OrderBy(i => i, new ItemPairComparer());
            foreach (var kv in sorted)
            {
                if (kv.Value < 0)
                    listContents += $"{kv.Key.UILinkAndNumber(kv.Value * -1)}\n";
            }
            foreach (var kv in tagsRemaining)
            {
                if (kv.Value < 0)
                    listContents += $"{kv.Key.UILinkAndNumber(kv.Value * -1)}\n";
            }

            ChatBase.Send(new ChatBase.InfoPane(
                "Your Projects are Missing These Items",
                listContents,
                "needed-items",
                user,
                ChatBase.PanelType.InfoPanel,
                false,
                false
            ));
        }
    }
}
