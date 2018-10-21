using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Objects;
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

            if (((i is Tool) || (i is Ring) || (i is Boots)) && i.canBeTrashed())
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
        static bool Prefix(IList<Item> items, ref ShippingMenu __instance)
        {
            List<int> categoryTotals = ModEntry.ModHelper.Reflection.GetField<List<int>>(__instance, "categoryTotals").GetValue();
            List<List<Item>> categoryItems = ModEntry.ModHelper.Reflection.GetField<List<List<Item>>>(__instance, "categoryItems").GetValue();
            List<MoneyDial> categoryDials = ModEntry.ModHelper.Reflection.GetField<List<MoneyDial>>(__instance, "categoryDials").GetValue();
            Utility.consolidateStacks(items);
            for (int index = 0; index < 6; ++index)
            {
                categoryItems.Add(new List<Item>());
                categoryTotals.Add(0);
                categoryDials.Add(new MoneyDial(7, index == 5));
            }
            foreach (Item obj in (IEnumerable<Item>)items)
            {
                if (obj is StardewValley.Object)
                {
                    StardewValley.Object o = obj as StardewValley.Object;
                    int categoryIndexForObject = __instance.getCategoryIndexForObject(o);
                    categoryItems[categoryIndexForObject].Add((Item)o);
                    categoryTotals[categoryIndexForObject] += o.sellToStorePrice() * o.Stack;
                    Game1.stats.itemsShipped += (uint)o.Stack;
                    if (o.Category == -75 || o.Category == -79)
                        Game1.stats.CropsShipped += (uint)o.Stack;
                    if (o.countsForShippedCollection())
                        Game1.player.shippedBasic((int)((NetFieldBase<int, NetInt>)o.parentSheetIndex), (int)((NetFieldBase<int, NetInt>)o.stack));
                }
                else if (((obj is Tool) || (obj is Ring) || (obj is Boots)) && obj.canBeTrashed())
                {
                    //int categoryIndexForObject = __instance.getCategoryIndexForObject(obj);
                    categoryItems[4].Add(obj);
                    categoryTotals[4] += (obj.salePrice()/2) * obj.Stack;
                    Game1.stats.itemsShipped += (uint)obj.Stack;
                }
            }
            for (int index = 0; index < 5; ++index)
            {
                categoryTotals[5] += categoryTotals[index];
                categoryItems[5].AddRange((IEnumerable<Item>)categoryItems[index]);
                categoryDials[index].currentValue = categoryTotals[index];
                categoryDials[index].previousTargetValue = categoryDials[index].currentValue;
            }
            categoryDials[5].currentValue = categoryTotals[5];
            if (Game1.IsMasterGame)
                Game1.player.Money += categoryTotals[5];
            Game1.setRichPresence("earnings", (object)categoryTotals[5]);
            return false;
        }
    }

    [HarmonyPatch(typeof(ShippingMenu))]
    [HarmonyPatch("draw")]
    class CustomDraw
    {
        static bool Prefix(SpriteBatch b, ref ShippingMenu __instance)
        {
            List<int> categoryTotals = ModEntry.ModHelper.Reflection.GetField<List<int>>(__instance, "categoryTotals").GetValue();
            List<List<Item>> categoryItems = ModEntry.ModHelper.Reflection.GetField<List<List<Item>>>(__instance, "categoryItems").GetValue();
            List<MoneyDial> categoryDials = ModEntry.ModHelper.Reflection.GetField<List<MoneyDial>>(__instance, "categoryDials").GetValue();
            int introTimer = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "introTimer").GetValue();
            int categoryLabelsWidth = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "categoryLabelsWidth").GetValue();
            int plusButtonWidth = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "plusButtonWidth").GetValue();
            int itemSlotWidth = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "itemSlotWidth").GetValue();
            int itemAndPlusButtonWidth = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "itemAndPlusButtonWidth").GetValue();
            int totalWidth = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "totalWidth").GetValue();
            int centerX = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "centerX").GetValue();
            int centerY = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "centerY").GetValue();
            int outroFadeTimer = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "outroFadeTimer").GetValue();
            int outroPauseBeforeDateChange = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "outroPauseBeforeDateChange").GetValue();
            int finalOutroTimer = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "finalOutroTimer").GetValue();
            int smokeTimer = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "smokeTimer").GetValue();
            int dayPlaqueY = ModEntry.ModHelper.Reflection.GetField<int>(__instance, "dayPlaqueY").GetValue();
            float weatherX = ModEntry.ModHelper.Reflection.GetField<float>(__instance, "weatherX").GetValue();
            bool outro = ModEntry.ModHelper.Reflection.GetField<bool>(__instance, "outro").GetValue();
            bool newDayPlaque = ModEntry.ModHelper.Reflection.GetField<bool>(__instance, "newDayPlaque").GetValue();
            bool savedYet = ModEntry.ModHelper.Reflection.GetField<bool>(__instance, "savedYet").GetValue();
            SaveGameMenu saveGameMenu = ModEntry.ModHelper.Reflection.GetField<SaveGameMenu>(__instance, "saveGameMenu").GetValue();

            if (Game1.wasRainingYesterday)
            {
                b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), new Rectangle?(new Rectangle(639, 858, 1, 184)), Game1.currentSeason.Equals("winter") ? Color.LightSlateGray : Color.SlateGray * (float)(1.0 - (double)introTimer / 3500.0));
                b.Draw(Game1.mouseCursors, new Rectangle(2556, 0, Game1.viewport.Width, Game1.viewport.Height), new Rectangle?(new Rectangle(639, 858, 1, 184)), Game1.currentSeason.Equals("winter") ? Color.LightSlateGray : Color.SlateGray * (float)(1.0 - (double)introTimer / 3500.0));
                int num1 = -244;
                while (num1 < Game1.viewport.Width + 244)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)num1 + (float)((double)weatherX / 2.0 % 244.0), 32f), new Rectangle?(new Rectangle(643, 1142, 61, 53)), Color.DarkSlateGray * 1f * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                    num1 += 244;
                }
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, (float)(Game1.viewport.Height - 192)), new Rectangle?(new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 48)), (Game1.currentSeason.Equals("winter") ? Color.White * 0.25f : new Color(30, 62, 50)) * (float)(0.5 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, (float)(Game1.viewport.Height - 192)), new Rectangle?(new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 48)), (Game1.currentSeason.Equals("winter") ? Color.White * 0.25f : new Color(30, 62, 50)) * (float)(0.5 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, (float)(Game1.viewport.Height - 128)), new Rectangle?(new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 32)), (Game1.currentSeason.Equals("winter") ? Color.White * 0.5f : new Color(30, 62, 50)) * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, (float)(Game1.viewport.Height - 128)), new Rectangle?(new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 32)), (Game1.currentSeason.Equals("winter") ? Color.White * 0.5f : new Color(30, 62, 50)) * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(160f, (float)(Game1.viewport.Height - 128 + 16 + 8)), new Rectangle?(new Rectangle(653, 880, 10, 10)), Color.White * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                int num2 = -244;
                while (num2 < Game1.viewport.Width + 244)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)num2 + weatherX % 244f, -32f), new Rectangle?(new Rectangle(643, 1142, 61, 53)), Color.SlateGray * 0.85f * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                    num2 += 244;
                }
                foreach (TemporaryAnimatedSprite animation in __instance.animations)
                    animation.draw(b, true, 0, 0, 1f);
                int num3 = -244;
                while (num3 < Game1.viewport.Width + 244)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)num3 + (float)((double)weatherX * 1.5 % 244.0), (float)sbyte.MinValue), new Rectangle?(new Rectangle(643, 1142, 61, 53)), Color.LightSlateGray * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                    num3 += 244;
                }
            }
            else
            {
                b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), new Rectangle?(new Rectangle(639, 858, 1, 184)), Color.White * (float)(1.0 - (double)introTimer / 3500.0));
                b.Draw(Game1.mouseCursors, new Rectangle(2556, 0, Game1.viewport.Width, Game1.viewport.Height), new Rectangle?(new Rectangle(639, 858, 1, 184)), Color.White * (float)(1.0 - (double)introTimer / 3500.0));
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, 0.0f), new Rectangle?(new Rectangle(0, 1453, 639, 195)), Color.White * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, 0.0f), new Rectangle?(new Rectangle(0, 1453, 639, 195)), Color.White * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                if (Game1.dayOfMonth == 28)
                    b.Draw(Game1.mouseCursors, new Vector2((float)(Game1.viewport.Width - 176), 4f), new Rectangle?(new Rectangle(642, 835, 43, 43)), Color.White * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, (float)(Game1.viewport.Height - 192)), new Rectangle?(new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 48)), (Game1.currentSeason.Equals("winter") ? Color.White * 0.25f : new Color(0, 20, 40)) * (float)(0.649999976158142 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, (float)(Game1.viewport.Height - 192)), new Rectangle?(new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 48)), (Game1.currentSeason.Equals("winter") ? Color.White * 0.25f : new Color(0, 20, 40)) * (float)(0.649999976158142 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, (float)(Game1.viewport.Height - 128)), new Rectangle?(new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 32)), (Game1.currentSeason.Equals("winter") ? Color.White * 0.5f : new Color(0, 32, 20)) * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, (float)(Game1.viewport.Height - 128)), new Rectangle?(new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 32)), (Game1.currentSeason.Equals("winter") ? Color.White * 0.5f : new Color(0, 32, 20)) * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(160f, (float)(Game1.viewport.Height - 128 + 16 + 8)), new Rectangle?(new Rectangle(653, 880, 10, 10)), Color.White * (float)(1.0 - (double)introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            }
            if (!outro && !Game1.wasRainingYesterday)
            {
                foreach (TemporaryAnimatedSprite animation in __instance.animations)
                    animation.draw(b, true, 0, 0, 1f);
            }
            if (__instance.currentPage == -1)
            {
                SpriteText.drawStringWithScrollCenteredAt(b, Utility.getYesterdaysDate(), Game1.viewport.Width / 2, __instance.categories[0].bounds.Y - 128, "", 1f, -1, 0, 0.88f, false);
                int num = -20;
                int index1 = 0;
                foreach (ClickableTextureComponent category in __instance.categories)
                {
                    if (introTimer < 2500 - index1 * 500)
                    {
                        Vector2 vector2 = category.getVector2() + new Vector2(12f, -8f);
                        if (category.visible)
                        {
                            category.draw(b);
                            b.Draw(Game1.mouseCursors, vector2 + new Vector2(-104f, (float)(num + 4)), new Rectangle?(new Rectangle(293, 360, 24, 24)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
                            categoryItems[index1][0].drawInMenu(b, vector2 + new Vector2(-88f, (float)(num + 16)), 1f, 1f, 0.9f, false);
                        }
                        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), (int)((double)vector2.X + (double)-itemSlotWidth - (double)categoryLabelsWidth - 12.0), (int)((double)vector2.Y + (double)num), categoryLabelsWidth, 104, Color.White, 4f, false);
                        SpriteText.drawString(b, category.hoverText, (int)vector2.X - itemSlotWidth - categoryLabelsWidth + 8, (int)vector2.Y + 4, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
                        for (int index2 = 0; index2 < 6; ++index2)
                            b.Draw(Game1.mouseCursors, vector2 + new Vector2((float)(-itemSlotWidth - 192 - 24 + index2 * 6 * 4), 12f), new Rectangle?(new Rectangle(355, 476, 7, 11)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
                        categoryDials[index1].draw(b, vector2 + new Vector2((float)(-itemSlotWidth - 192 - 48 + 4), 20f), categoryTotals[index1]);
                        b.Draw(Game1.mouseCursors, vector2 + new Vector2((float)(-itemSlotWidth - 64 - 4), 12f), new Rectangle?(new Rectangle(408, 476, 9, 11)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
                    }
                    ++index1;
                }
                if (introTimer <= 0)
                    __instance.okButton.draw(b);
            }
            else
            {
                IClickableMenu.drawTextureBox(b, 0, 0, Game1.viewport.Width, Game1.viewport.Height, Color.White);
                Vector2 location = new Vector2((float)(__instance.xPositionOnScreen + 32), (float)(__instance.yPositionOnScreen + 32));
                for (int index = __instance.currentTab * 9; index < __instance.currentTab * 9 + 9; ++index)
                {
                    if (categoryItems[__instance.currentPage].Count > index)
                    {
                        categoryItems[__instance.currentPage][index].drawInMenu(b, location, 1f, 1f, 1f, true);
                        if (LocalizedContentManager.CurrentLanguageLatin)
                        {
                            SpriteText.drawString(b, categoryItems[__instance.currentPage][index].DisplayName + (categoryItems[__instance.currentPage][index].Stack > 1 ? " x" + (object)categoryItems[__instance.currentPage][index].Stack : ""), (int)location.X + 64 + 12, (int)location.Y + 12, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
                            string s = ".";
                            int num = 0;
                            while (num < __instance.width - 96 - SpriteText.getWidthOfString(categoryItems[__instance.currentPage][index].DisplayName + (categoryItems[__instance.currentPage][index].Stack > 1 ? " x" + (object)categoryItems[__instance.currentPage][index].Stack : "") + Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", (object)(getPrice(categoryItems[__instance.currentPage][index]) * categoryItems[__instance.currentPage][index].Stack))))
                            {
                                s += " .";
                                num += SpriteText.getWidthOfString(" .");
                            }
                            SpriteText.drawString(b, s, (int)location.X + 80 + SpriteText.getWidthOfString(categoryItems[__instance.currentPage][index].DisplayName + (categoryItems[__instance.currentPage][index].Stack > 1 ? " x" + (object)categoryItems[__instance.currentPage][index].Stack : "")), (int)location.Y + 8, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
                            SpriteText.drawString(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", (object)(getPrice(categoryItems[__instance.currentPage][index]) * categoryItems[__instance.currentPage][index].Stack)), (int)location.X + __instance.width - 64 - SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", (object)(getPrice(categoryItems[__instance.currentPage][index]) * categoryItems[__instance.currentPage][index].Stack))), (int)location.Y + 12, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
                        }
                        else
                        {
                            string s1 = categoryItems[__instance.currentPage][index].DisplayName + (categoryItems[__instance.currentPage][index].Stack > 1 ? " x" + (object)categoryItems[__instance.currentPage][index].Stack : ".");
                            string s2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", (object)(getPrice(categoryItems[__instance.currentPage][index]) * categoryItems[__instance.currentPage][index].Stack));
                            int x = (int)location.X + __instance.width - 64 - SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", (object)((getPrice(categoryItems[__instance.currentPage][index]) * categoryItems[__instance.currentPage][index].Stack))));
                            SpriteText.getWidthOfString(s1 + s2);
                            while (SpriteText.getWidthOfString(s1 + s2) < 1123)
                                s1 += " .";
                            if (SpriteText.getWidthOfString(s1 + s2) >= 1155)
                                s1 = s1.Remove(s1.Length - 1);
                            SpriteText.drawString(b, s1, (int)location.X + 64 + 12, (int)location.Y + 12, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
                            SpriteText.drawString(b, s2, x, (int)location.Y + 12, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
                        }
                        location.Y += 68f;
                    }
                }
                __instance.backButton.draw(b);
                if (__instance.showForwardButton())
                    __instance.forwardButton.draw(b);
            }
            if (outro)
            {
                b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), new Rectangle?(new Rectangle(639, 858, 1, 184)), Color.Black * (float)(1.0 - (double)outroFadeTimer / 800.0));
                SpriteText.drawStringWithScrollCenteredAt(b, newDayPlaque ? Utility.getDateString(0) : Utility.getYesterdaysDate(), Game1.viewport.Width / 2, dayPlaqueY, "", 1f, -1, 0, 0.88f, false);
                foreach (TemporaryAnimatedSprite animation in __instance.animations)
                    animation.draw(b, true, 0, 0, 1f);
                if (finalOutroTimer > 0)
                    b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), new Rectangle?(new Rectangle(0, 0, 1, 1)), Color.Black * (float)(1.0 - (double)finalOutroTimer / 2000.0));
            }
            if (saveGameMenu != null)
                saveGameMenu.draw(b);
            if (Game1.options.SnappyMenus && (introTimer > 0 || outro)) return false;
            __instance.drawMouse(b);
            return false;
        }

        static int getPrice(Item i)
        {
            return (i is Object) ? (i as Object).sellToStorePrice() : i.salePrice() / 2;
        }
    }
}
