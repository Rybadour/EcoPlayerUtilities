using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;

namespace EcoRebalanced
{
    class Utils
    {
        public static IEnumerable<T> getOwnedComponents<T>(User user)
        {
            /* *
            foreach (Deed deed in user.GetAllProperty())
            {
                foreach (WorldObject wo in deed.GetObjectsCreatedByUser(user))
                {
                    StorageComponent storage = wo.GetComponent<StorageComponent>();
            /* */
            return user.GetAllProperty().SelectMany(p =>
                p.GetObjectsCreatedByUser(user)
                    .Select(o => o.GetComponent<T>())
                    .Where(c => c != null)
            );
        }

        public static SortedDictionary<string, int> getAllUserItems(User user, string tagFilter)
        {
            SortedDictionary<string, int> itemTotals = new SortedDictionary<string, int>();
            foreach (StorageComponent storage in getOwnedComponents<StorageComponent>(user))
            {
                foreach (ItemStack stack in storage.Inventory.NonEmptyStacks)
                {
                    if (tagFilter != "ALL" && !stack.Item.TagNames().Contains(tagFilter)) continue;

                    int total;
                    bool contains = itemTotals.TryGetValue(stack.DisplayName(), out total);
                    if (!contains) total = 0;

                    itemTotals[stack.DisplayName()] = total + stack.Quantity;
                }
            }

            return itemTotals;
        }

        // Subtracts some amount of some item. If there was less items than wanted to be subtracted the remainder is returned.
        // If stopAtZero is true then the item amount will not go into the negative.
        public static int subtractFromItem(IDictionary<string, int> items, string itemName, int amountToSubtract, bool stopAtZero)
        {
            int itemAmount;
            bool contains = items.TryGetValue(itemName, out itemAmount);
            if (contains)
            {
                if (stopAtZero && itemAmount < amountToSubtract)
                {
                    items[itemName] = 0;
                }
                else
                {
                    items[itemName] = itemAmount - amountToSubtract;
                }
            }
            else if (!stopAtZero)
            {
                items.Add(itemName, -amountToSubtract);
                itemAmount = 0;
            }

            return (itemAmount > amountToSubtract ? 0 : amountToSubtract - itemAmount);
        }
    }
}
