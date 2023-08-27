using HarmonyLib;
using System;
using Verse;
using System.Reflection;
using RoadsOfTheRim;

namespace VOEAdditionalOutposts_RoadsOfTheRim
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType;

        static HarmonyPatches()
        {
            patchType = typeof(HarmonyPatches);
            Harmony val = new Harmony("rimworld.mrhydralisk.VOEAdditionalOutposts.RoadsOfTheRimPatch");

            val.Patch((MethodBase)AccessTools.Method(typeof(RoadsOfTheRim.RoadsOfTheRim), "FinaliseConstructionSite", (Type[])null, (Type[])null), new HarmonyMethod(patchType, "RotR_FinaliseConstructionSite_Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
        }
        public static bool RotR_FinaliseConstructionSite_Prefix(RoadConstructionSite site, RoadsOfTheRim.RoadsOfTheRim __instance)
        {
            if (RoadsOfTheRim.RoadsOfTheRim.RoadBuildingState.Caravan  == null)
            {
                if (site.GetNextLeg() != null)
                {
                    site.GetComponent<WorldObjectComp_ConstructionSite>().SetCosts();
                }
                else
                {
                    RoadConstructionSite.DeleteSite(site);
                }
                return false;
            }
            return true;
        }
    }
}
