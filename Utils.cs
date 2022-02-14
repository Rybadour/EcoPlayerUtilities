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
            return WorldObjectManager.GetOwnedBy(user)
                .SelectMany(wo => wo.GetComponents<T>())
                .Where(c => c != null);
        }

        public static Dictionary<int, int> getAllUserItems(User user, string tagFilter, bool includeBackpack)
        {
            Dictionary<int, int> itemTotals = new Dictionary<int, int>();
            IEnumerable<ItemStack> stacks = getOwnedComponents<StorageComponent>(user)
                .SelectMany(s => s.Inventory.NonEmptyStacks);
            if (includeBackpack)
            {
                stacks = stacks.Concat(user.Inventory.Backpack.NonEmptyStacks)
                    .Concat(user.Inventory.Carried.NonEmptyStacks)
                    .Concat(user.Inventory.Toolbar.NonEmptyStacks);
            }

            foreach (ItemStack stack in stacks)
            {
                if (tagFilter != "ALL" && !stack.Item.TagNames().Contains(tagFilter)) continue;

                int total;
                bool contains = itemTotals.TryGetValue(stack.Item.TypeID, out total);
                if (!contains) total = 0;

                itemTotals[stack.Item.TypeID] = total + stack.Quantity;
            }

            return itemTotals;
        }

        public static void addToDictionaryValue<T>(IDictionary<T, int> dict, T key, int amount)
        {
            int itemAmount;
            bool contains = dict.TryGetValue(key, out itemAmount);
            if (contains)
                dict[key] = itemAmount + amount;
            else
                dict.Add(key, amount);
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

    public class ItemPairComparer : IComparer<KeyValuePair<int, int>>
    {
        public int Compare(KeyValuePair<int, int> a, KeyValuePair<int, int> b)
        {
            Item itemA = Item.Get(a.Key);
            Item itemB = Item.Get(b.Key);
            return itemA.DisplayName.CompareTo(itemB.DisplayName);
        }
    }
}
