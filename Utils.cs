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

        public static Dictionary<Item, int> getAllUserItems(User user, string tagFilter)
        {
            Dictionary<Item, int> itemTotals = new Dictionary<Item, int>();
            foreach (StorageComponent storage in getOwnedComponents<StorageComponent>(user))
            {
                foreach (ItemStack stack in storage.Inventory.NonEmptyStacks)
                {
                    if (tagFilter != "ALL" && !stack.Item.TagNames().Contains(tagFilter)) continue;

                    int total;
                    bool contains = itemTotals.TryGetValue(stack.Item, out total);
                    if (!contains) total = 0;

                    itemTotals[stack.Item] = total + stack.Quantity;
                }
            }

            return itemTotals;
        }

        // Subtracts some amount of some item. If there was less items than wanted to be subtracted the remainder is returned.
        // If stopAtZero is true then the item amount will not go into the negative.
        public static int subtractFromDictionaryValue<T>(IDictionary<T, int> dict, T key, int amountToSubtract, bool stopAtZero)
        {
            int itemAmount;
            bool contains = dict.TryGetValue(key, out itemAmount);
            if (contains)
            {
                if (stopAtZero && itemAmount < amountToSubtract)
                {
                    dict[key] = 0;
                }
                else
                {
                    dict[key] = itemAmount - amountToSubtract;
                }
            }
            else if (!stopAtZero)
            {
                dict.Add(key, -amountToSubtract);
                itemAmount = 0;
            }

            return (itemAmount > amountToSubtract ? 0 : amountToSubtract - itemAmount);
        }
    }

    public class ItemPairComparer : IComparer<KeyValuePair<Item, int>>
    {
        public int Compare(KeyValuePair<Item, int> a, KeyValuePair<Item, int> b)
        {
            return a.Key.DisplayName.CompareTo(b.Key.DisplayName);
        }
    }
}
