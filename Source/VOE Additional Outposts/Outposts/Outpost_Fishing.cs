using Outposts;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts
{
    public class Outpost_Fishing : Outpost
    {
        private List<FishChance> possibleFishCommon;
        private List<FishChance> possibleFishUncommon;

        [PostToSetings("Outposts.Settings.Production", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 5f, null, null)]
        public float ProductionMultiplier = 1f;

        [PostToSetings("VOEAdditionalOutposts.Settings.RestPercent", PostToSetingsAttribute.DrawMode.Percentage, 0.33f, 0f, 1f, null, null)]
        public float RestPercent = 0.33f;

        public override IEnumerable<Thing> ProducedThings()
        {
            List<Thing> list = new List<Thing>();
            foreach (Pawn capablePawn in CapablePawns)
            {
                float ticksPerCatch = 7500f / (capablePawn.GetStatValue(StatDefOf.FishingSpeed) * (1 - RestPercent));
                float curTick = 0;
                int lastRareCatchTick = 0;
                while (curTick < TicksPerProduction)
                {
                    list.AddRange(GetCatchesFor(capablePawn, (curTick - lastRareCatchTick) / 300000, out bool isRare));
                    curTick += ticksPerCatch;
                    if (isRare)
                    {
                        lastRareCatchTick = (int)curTick;
                    }
                }
            }
            return list.GroupBy((Thing t) => t.def).SelectMany(g => g.Key.Make(g.Sum((Thing t) => t.stackCount)));
        }

        public List<Thing> GetCatchesFor(Pawn pawn, float rareChance, out bool isRare)
        {
            List<Thing> tmpCatches = new List<Thing>();
            isRare = false;
            if (!ModsConfig.OdysseyActive)
            {
                return tmpCatches;
            }
            if (Biome.fishTypes.rareCatchesSetMaker != null && Rand.Chance(rareChance * 0.01f))
            {
                tmpCatches.AddRange(Biome.fishTypes.rareCatchesSetMaker.root.Generate());
                if (tmpCatches.Any())
                {
                    isRare = true;
                    return tmpCatches;
                }
            }
            isRare = false;
            if ((!Rand.Chance(0.05f) || !possibleFishUncommon.TryRandomElementByWeight((FishChance fc) => fc.chance, out var result)) && !possibleFishCommon.TryRandomElementByWeight((FishChance fc) => fc.chance, out result))
            {
                return tmpCatches;
            }
            Thing thing = ThingMaker.MakeThing(result.fishDef);
            thing.stackCount = Mathf.Max(1, Mathf.RoundToInt(6 * pawn.GetStatValue(StatDefOf.FishingYield)));
            tmpCatches.Add(thing);
            return tmpCatches;
        }

        public override void RecachePawnTraits()
        {
            base.RecachePawnTraits();
            possibleFishCommon = new List<FishChance>();
            possibleFishUncommon = new List<FishChance>();
            if (Find.WorldGrid[this.Tile] is SurfaceTile surfaceTile)
            {
                if (!surfaceTile.Rivers.NullOrEmpty())
                {
                    possibleFishCommon.AddRange(Biome.fishTypes.freshwater_Common);
                    possibleFishUncommon.AddRange(Biome.fishTypes.freshwater_Uncommon);
                }
                if (Find.World.CoastDirectionAt(this.Tile) != Rot4.Invalid)
                {
                    possibleFishCommon.AddRange(Biome.fishTypes.saltwater_Common);
                    possibleFishUncommon.AddRange(Biome.fishTypes.saltwater_Uncommon);
                }
                if (ModsConfig.BiotechActive && surfaceTile.pollution > 0)
                {
                    possibleFishCommon.Add(new FishChance() { fishDef = ThingDefOf.Fish_Toxfish, chance = surfaceTile.pollution });
                }
            }
        }

        public int AproxFish(out int fishingTimes)
        {
            int amount = 0;
            float amountOfFishing = 0;
            foreach (Pawn capablePawn in CapablePawns)
            {
                float amountOfPawnFishing = (TicksPerProduction * capablePawn.GetStatValue(StatDefOf.FishingSpeed) / 7500f) * (1 - RestPercent);
                amountOfFishing += amountOfPawnFishing;
                amount += Mathf.RoundToInt(amountOfPawnFishing * Mathf.Max(1, 6 * capablePawn.GetStatValue(StatDefOf.FishingYield)));
            }
            fishingTimes = (int)amountOfFishing;
            return amount;
        }

        public override string ProductionString()
        {
            List<string> productionStrings = new List<string>();
            productionStrings.Add("VOEAdditionalOutposts.AproxFish".Translate(AproxFish(out int fishingTimes), fishingTimes));
            productionStrings.Add("VOEAdditionalOutposts.AvailableFish".Translate());
            if (!possibleFishCommon.NullOrEmpty())
            {
                productionStrings.Add(string.Join("\n", possibleFishCommon.Select((FishChance fc) => fc.fishDef.label)));
            }
            if (!possibleFishUncommon.NullOrEmpty())
            {
                productionStrings.Add(string.Join("\n", possibleFishUncommon.Select((FishChance fc) => fc.fishDef.label)));
            }
            return String.Join("\n", productionStrings);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref possibleFishCommon, "possibleFishCommon", LookMode.Value);
            Scribe_Collections.Look(ref possibleFishUncommon, "possibleFishUncommon", LookMode.Value);
        }

        public static string CanSpawnOnWith(PlanetTile tile, List<Pawn> pawns)
        {
            if (Find.WorldGrid[tile].Isnt<SurfaceTile>(out var surfaceTile) || (surfaceTile.Rivers.NullOrEmpty() && Find.World.CoastDirectionAt(tile) == Rot4.Invalid))
            {
                return "Outposts.MustBeOnCoastOrRiver".Translate();
            }
            return (TaggedString)null;
        }

        public static string RequirementsString(PlanetTile tile, List<Pawn> pawns)
        {
            return "Outposts.MustBeOnCoastOrRiver".Translate().Requirement(Find.WorldGrid[tile] is SurfaceTile surfaceTile && !(surfaceTile.Rivers.NullOrEmpty() && Find.World.CoastDirectionAt(tile) == Rot4.Invalid));
        }
    }
}