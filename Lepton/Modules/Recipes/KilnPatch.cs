using Harmony;
using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Cael.Lepton.Modules.Recipes
{
    [HarmonyPatch(typeof(SObject))]
    [HarmonyPatch("performObjectDropInAction")]
    class CustomPerformObjectDropInAction
    {
        static bool Prefix(ref SObject __instance, ref bool __result, ref Item dropInItem, bool probe, Farmer who)
        {
            if (!(dropInItem is SObject))
                return false;
            SObject object1 = dropInItem as SObject;

            //if the item is wood we still want it to continue to the normal kiln operation
            if (object1.ParentSheetIndex == 388) return true;

            //Machine is a Kiln
            if (__instance.name.Equals("Charcoal Kiln"))
            {
                //Item is not hardwood
                if (who.IsLocalPlayer && object1.ParentSheetIndex != 709)
                {
                    if (!probe && who.IsLocalPlayer)
                        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12783"));
                    return false;
                }
                //Coal was not produced yet and the machine is not running
                if (__instance.heldObject.Value == null && !probe)
                {
                    object1.Stack -= 10;
                    if (object1.Stack <= 0)
                        who.removeItemFromInventory((Item)object1);
                    __instance.heldObject.Value = new SObject(382, 5, false, -1, 0);
                    __instance.MinutesUntilReady = 150;
                    who.currentLocation.playSound("openBox");
                    DelayedAction.playSoundAfterDelay("fireball", 50, (GameLocation)null);
                    __instance.showNextIndex.Value = true;
                    Multiplayer multiplayer = ModEntry.ModHelper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
                    multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite[1]
                      {
                        new TemporaryAnimatedSprite(27, __instance.TileLocation * 64f + new Vector2(-16f, (float) sbyte.MinValue), Color.White, 4, false, 50f, 10, 64, (float) (((double) __instance.TileLocation.Y + 1.0) * 64.0 / 10000.0 + 9.99999974737875E-05), -1, 0)
                        {
                          alphaFade = 0.005f
                        }
                      });
                    __result = object1.Stack <= 0;
                    return false;
                }
                else if (__instance.heldObject.Value == null & probe)
                {
                    if (object1.ParentSheetIndex == 709)
                    {
                        __instance.heldObject.Value = new SObject();
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }
    }
}
