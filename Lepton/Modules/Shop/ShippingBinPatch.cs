using Harmony;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Tools;
using System.Collections.Generic;

namespace Cael.Lepton.Modules.Shop
{
    [HarmonyPatch(typeof(Utility))]
    [HarmonyPatch("highlightShippableObjects")]
    class CustomHighlightShippableObjects
    {
        static bool Prefix(Item i, ref bool __result)
        {
            Mod modInstance = ModEntry.ModInstance;

            if (i is Tool && i.canBeTrashed())
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShippingMenu))]
    [HarmonyPatch("parseItems")]
    class CustomParseItems
    {
        static bool Prefix(IList<Item> items, ref ShippingMenu __instance, ref List<int> ___categoryTotals, ref List<MoneyDial> ___categoryDials, ref List<List<Item>> ___categoryItems)
        {
            Utility.consolidateStacks(items);
            for (int index = 0; index < 6; ++index)
            {
                ___categoryItems.Add(new List<Item>());
                ___categoryTotals.Add(0);
                ___categoryDials.Add(new MoneyDial(7, index == 5));
            }
            foreach (Item obj in (IEnumerable<Item>)items)
            {
                if (obj is StardewValley.Object)
                {
                    StardewValley.Object o = obj as StardewValley.Object;
                    int categoryIndexForObject = __instance.getCategoryIndexForObject(o);
                    ___categoryItems[categoryIndexForObject].Add((Item)o);
                    ___categoryTotals[categoryIndexForObject] += o.sellToStorePrice() * o.Stack;
                    Game1.stats.itemsShipped += (uint)o.Stack;
                    if (o.Category == -75 || o.Category == -79)
                        Game1.stats.CropsShipped += (uint)o.Stack;
                    if (o.countsForShippedCollection())
                        Game1.player.shippedBasic((int)((NetFieldBase<int, NetInt>)o.parentSheetIndex), (int)((NetFieldBase<int, NetInt>)o.stack));
                }
                else if (obj is Tool && obj.canBeTrashed())
                {
                    //int categoryIndexForObject = __instance.getCategoryIndexForObject(obj);
                    ___categoryItems[1].Add(obj);
                    ___categoryTotals[1] += obj.salePrice() * obj.Stack;
                    Game1.stats.itemsShipped += (uint)obj.Stack;
                }
            }
            for (int index = 0; index < 5; ++index)
            {
                ___categoryTotals[5] += ___categoryTotals[index];
                ___categoryItems[5].AddRange((IEnumerable<Item>)___categoryItems[index]);
                ___categoryDials[index].currentValue = ___categoryTotals[index];
                ___categoryDials[index].previousTargetValue = ___categoryDials[index].currentValue;
            }
            ___categoryDials[5].currentValue = ___categoryTotals[5];
            if (Game1.IsMasterGame)
                Game1.player.Money += ___categoryTotals[5];
            Game1.setRichPresence("earnings", (object)___categoryTotals[5]);
            return false;
        }
    }
}
