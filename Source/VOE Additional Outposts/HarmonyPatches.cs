using HarmonyLib;
using Outposts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace VOEAdditionalOutposts
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType;

        private static AccessTools.FieldRef<object, List<Thing>> containedItems;

        static HarmonyPatches()
        {
            patchType = typeof(HarmonyPatches);
            containedItems = AccessTools.FieldRefAccess<List<Thing>>(typeof(Outpost), "containedItems");
            Harmony val = new Harmony("rimworld.mrhydralisk.VOEPowerGrid.Outpost.TakeItems");
            val.Patch((MethodBase)AccessTools.Method(typeof(Outpost), "TakeItems", new Type[] { typeof(ThingDef), typeof(int) }, (Type[])null), new HarmonyMethod(patchType, "Outpost_TakeItems_Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
        }
        public static bool Outpost_TakeItems_Prefix(ThingDef thingDef, int stackCount, ref List<Thing> __result, Outpost __instance)
        {
            List<Thing> list = new List<Thing>();
            ref List<Thing> reference = ref containedItems.Invoke((object)__instance);
            while (stackCount > 0)
            {
                Thing item = reference.FirstOrDefault((Thing t) => t.def == thingDef && t.stackCount > 0);
                if (item != null)
                {
                    if (stackCount < item.stackCount)
                    {
                        list.Add(item.SplitOff(stackCount));
                        stackCount = 0;
                    }
                    else
                    {
                        stackCount -= item.stackCount;
                        list.Add(__instance.TakeItem(item));
                    }
                }
                else
                {
                    break;
                }
            }
            __result = list;
            return false;
        }
    }
}
