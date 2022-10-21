using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Outposts;
using System.Reflection;
using RimWorld.Planet;

namespace VOEAdditionalOutposts
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType;

        static HarmonyPatches()
        {
            patchType = typeof(HarmonyPatches);
            Harmony val = new Harmony("rimworld.mrhydralisk.VOEAdditionalOutposts.Outpost");
            val.Patch((MethodBase)AccessTools.Method(typeof(Outpost), "GetCaravanGizmos", new Type[] { typeof(Caravan) }, (Type[])null), (HarmonyMethod)null, new HarmonyMethod(patchType, "Outpost_GetCaravanGizmos_Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null);
        }
        public static void Outpost_GetCaravanGizmos_Postfix(Caravan caravan, ref IEnumerable<Gizmo> __result, Outpost __instance)
        {
            List<Gizmo> NGizmos = __result.ToList();
            NGizmos.Insert(NGizmos.FindIndex((Gizmo g) => (g as Command_Action).defaultLabel == "Outposts.Commands.AddPawn.Label".Translate()) + 1, new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new FloatMenu(__instance.AllPawns.Select((Pawn p) => new FloatMenuOption(p.NameFullColored.CapitalizeFirst().Resolve(), delegate
                    {
                        __instance.RemovePawn(p);
                        caravan.AddPawnOrItem(p, true);
                        if (!p.IsWorldPawn())
                            Find.WorldPawns.PassToWorld(p);
                    })).ToList()));
                },
                defaultLabel = "VOEAdditionalOutposts.Commands.TakePawn.Label".Translate(),
                defaultDesc = "VOEAdditionalOutposts.Commands.TakePawn.Desc".Translate(),
                icon = Outposts.TexOutposts.RemoveTex
            });
            __result = NGizmos;
        }
    }
}
