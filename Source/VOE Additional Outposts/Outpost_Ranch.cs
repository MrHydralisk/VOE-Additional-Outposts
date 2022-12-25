using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Outposts;
using Verse;
using UnityEngine;
using RimWorld.Planet;

namespace VOEAdditionalOutposts
{
    public class Outpost_Ranch : Outpost
    {
        [PostToSetings("VOEAdditionalOutposts.Settings.BodySize", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.1f, 5f, null, null)]
        public float BodyScale = 1f;

        [PostToSetings("VOEAdditionalOutposts.Settings.FemaleAnimalPercent", PostToSetingsAttribute.DrawMode.Percentage, 0.5f, 0.1f, 1f, null, null)]
        public float FemaleAnimalPercent = 0.5f;

        [PostToSetings("VOEAdditionalOutposts.Settings.BaseHerdSize", PostToSetingsAttribute.DrawMode.IntSlider, 2, 2, 10, null, null)]
        public int BaseHerdSize = 2;

        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;

        private ThingDef animalThingDef;

        private bool iSEggLayer => animalThingDef.HasComp(typeof(CompEggLayer));

        private int animalCount = 1;

        private static readonly SimpleCurve animalMaxCurve = new SimpleCurve
        {
            new CurvePoint(0.2f, 22f),
            new CurvePoint(0.3f, 18f),
            new CurvePoint(0.75f, 12f),
            new CurvePoint(1f, 7f),
            new CurvePoint(2.4f, 5f),
            new CurvePoint(4f, 3f),
            new CurvePoint(6f, 1f)
        };

        private int animalMax => Mathf.CeilToInt((CapablePawns.Sum((Pawn p) => p.skills.GetSkill(SkillDefOf.Animals).Level) / 20f) * animalMaxCurve.Evaluate(animalThingDef.race.baseBodySize * BodyScale)) + BaseHerdSize;

        private int animalSlaughtered;

        private int ticksPerBirth => (int)((iSEggLayer ? animalThingDef.GetCompProperties<CompProperties_EggLayer>().eggLayIntervalDays : animalThingDef.race.gestationPeriodDays) * 60000f / Mathf.Ceil(animalCount * FemaleAnimalPercent));

        private int ticksTillBirth;

        public override List<ResultOption> ResultOptions
        {
            get
            {
                List<ResultOption> resultOptions = new List<ResultOption>();
                if (animalThingDef == null)
                {
                    return resultOptions;
                }
                if (animalCount > animalMax)
                {
                    animalSlaughtered += animalCount - animalMax;
                    animalCount = animalMax;
                }
                if (animalSlaughtered > 0)
                {
                    float ButcherScale = TicksPerProduction / (animalThingDef.race.lifeStageAges.Last().minAge * 3600000f);
                    if (animalThingDef.race.meatDef != null)
                    {
                        ResultOption ro = new ResultOption();
                        ro.Thing = animalThingDef.race.meatDef;
                        ro.BaseAmount = (int)(animalThingDef.GetStatValueAbstract(StatDefOf.MeatAmount) * animalSlaughtered * ButcherScale * OutpostsMod.Settings.ProductionMultiplier);
                        resultOptions.Add(ro);
                    }
                    if (animalThingDef.race.leatherDef != null)
                    {
                        ResultOption ro = new ResultOption();
                        ro.Thing = animalThingDef.race.leatherDef;
                        ro.BaseAmount = (int)(animalThingDef.GetStatValueAbstract(StatDefOf.LeatherAmount) * animalSlaughtered * ButcherScale * OutpostsMod.Settings.ProductionMultiplier);
                        resultOptions.Add(ro);
                    }
                }
                if (animalThingDef.HasComp(typeof(CompEggLayer)))
                {
                    CompProperties_EggLayer CP_EggLayer = animalThingDef.GetCompProperties<CompProperties_EggLayer>();
                    ResultOption ro = new ResultOption();
                    ro.Thing = CP_EggLayer.eggUnfertilizedDef;
                    ro.BaseAmount = (int)((TicksPerProduction / (CP_EggLayer.eggLayIntervalDays * 60000f)) * animalCount * CP_EggLayer.eggCountRange.RandomInRange * (CP_EggLayer.eggLayFemaleOnly ? FemaleAnimalPercent : 1f) * OutpostsMod.Settings.ProductionMultiplier);
                    resultOptions.Add(ro);
                }
                if (animalThingDef.HasComp(typeof(CompMilkable)))
                {
                    CompProperties_Milkable CP_Milkable = animalThingDef.GetCompProperties<CompProperties_Milkable>();
                    ResultOption ro = new ResultOption();
                    ro.Thing = CP_Milkable.milkDef;
                    ro.BaseAmount = (int)((TicksPerProduction / (CP_Milkable.milkIntervalDays * 60000f)) * animalCount * CP_Milkable.milkAmount * (CP_Milkable.milkFemaleOnly ? FemaleAnimalPercent : 1f) * OutpostsMod.Settings.ProductionMultiplier);
                    resultOptions.Add(ro);
                }
                if (animalThingDef.HasComp(typeof(CompShearable)))
                {
                    CompProperties_Shearable CP_Shearable = animalThingDef.GetCompProperties<CompProperties_Shearable>();
                    ResultOption ro = new ResultOption();
                    ro.Thing = CP_Shearable.woolDef;
                    ro.BaseAmount = (int)((TicksPerProduction / (CP_Shearable.shearIntervalDays * 60000f)) * animalCount * CP_Shearable.woolAmount  * OutpostsMod.Settings.ProductionMultiplier);
                    resultOptions.Add(ro);
                }
                return resultOptions;
            }
        }

        public override void Produce()
        {
            base.Produce();
            animalSlaughtered = 0;
        }

        public override void Tick()
        {
            base.Tick();
            if (animalThingDef == null)
            {
                ChooseAnimal();
            }
            else
            {
                ticksTillBirth--;
                if (ticksTillBirth <= 0)
                {
                    int animalNew = (int)(iSEggLayer ? Mathf.Min(animalThingDef.GetCompProperties<CompProperties_EggLayer>().eggCountRange.RandomInRange, animalThingDef.GetCompProperties<CompProperties_EggLayer>().eggFertilizationCountMax) : (animalThingDef.race.litterSizeCurve != null) ? Rand.ByCurve(animalThingDef.race.litterSizeCurve) : 1);
                    if (animalCount < animalMax)
                    {
                        if (animalCount + animalNew <= animalMax)
                        {
                            animalCount += animalNew;
                        }
                        else
                        {
                            animalSlaughtered += animalNew - (animalMax - animalCount);
                            animalCount = animalMax;
                        }
                    }
                    else
                    {
                        animalSlaughtered += animalNew;
                    }
                    ticksTillBirth = ticksPerBirth;
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (animalThingDef != null)
                yield return new Command_Action
                {
                    action = delegate
                    {
                        AddAnimal();
                    },
                    defaultLabel = ChooseExt.ChooseLabel,
                    defaultDesc = ChooseExt.ChooseDesc,
                    icon = animalThingDef.uiIcon,
                    disabled = AllPawns.Count((Pawn p) => p.def == animalThingDef) <= 0,
                    disabledReason = "VOEAdditionalOutposts.ChooseDisabledReason".Translate(animalThingDef.label)
                };
            if (Prefs.DevMode)
                yield return new Command_Action
                {
                    action = delegate
                    {
                        ticksTillBirth = 10;
                    },
                    defaultLabel = "Dev: Birth now",
                    defaultDesc = "Reduce ticksTillBirth to 10"
                };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref animalCount, "animalCount");
            Scribe_Values.Look(ref animalSlaughtered, "animalSlaughtered");
            Scribe_Values.Look(ref ticksTillBirth, "ticksTillBirth");
            Scribe_Defs.Look(ref animalThingDef, "animalThingDef");
        }

        public override string ProductionString()
        {
            string productionString = "";
            if (animalThingDef != null)
                productionString += "VOEAdditionalOutposts.WillHandleAnimals".Translate(animalThingDef.label, animalCount.ToStringSafe(), animalMax.ToStringSafe(), ticksTillBirth.ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor)).RawText;
            productionString += base.ProductionString();
            return  productionString;
        }

        private static ThingDef FindPairAnimal(List<Pawn> pawns)
        {
            List<Pawn> caravanAnimals = pawns.Where((Pawn p) => p.RaceProps.Animal).ToList();
            List<ThingDef> raceTypes = caravanAnimals.Select((Pawn p) => p.def).Distinct().ToList();
            foreach (ThingDef raceType in raceTypes)
            {
                if (raceType.race.hasGenders && caravanAnimals.Any((Pawn p) => p.def == raceType && p.gender == Gender.Female) && caravanAnimals.Any((Pawn p) => p.def == raceType && p.gender == Gender.Male))
                {
                    return raceType;
                }
            }
            return null;
        }

        public void AddAnimal()
        {
            int animalNew = AllPawns.Count((Pawn p) => p.def == animalThingDef);
            if (animalNew > 0)
            {
                if (animalCount + animalNew <= animalMax)
                {
                    animalCount += animalNew;
                }
                else
                {
                    animalSlaughtered += animalNew - (animalMax - animalCount);
                    animalCount = animalMax;
                }
                (AllPawns as List<Pawn>).RemoveAll((Pawn p) => p.def == animalThingDef);
                if (ticksTillBirth > ticksPerBirth)
                    ticksTillBirth = ticksPerBirth;
            }
        }

        public void ChooseAnimal()
        {
            List<Pawn> caravanAnimals = AllPawns.Where((Pawn p) => p.RaceProps.Animal).ToList();
            animalThingDef = FindPairAnimal(AllPawns.ToList());
            animalCount = AllPawns.Count((Pawn p) => p.def == animalThingDef) - 1;
            (AllPawns as List<Pawn>).RemoveAll((Pawn p) => p.def == animalThingDef);
        }

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns)
        {
            return (FindPairAnimal(Find.WorldObjects.Caravans.Where((Caravan c) => c.Tile == tile && c.IsPlayerControlled && c.ContainsPawn(pawns.FirstOrDefault())).FirstOrDefault().PawnsListForReading) == null ? "VOEAdditionalOutposts.MustBeMade.PairAnimal".Translate() : ((TaggedString)null));
        }

        public static string RequirementsString(int tile, List<Pawn> pawns)
        {
            return "VOEAdditionalOutposts.MustBeMade.PairAnimal".Translate().Requirement(FindPairAnimal(Find.WorldObjects.Caravans.Where((Caravan c) => c.Tile == tile && c.IsPlayerControlled && c.ContainsPawn(pawns.FirstOrDefault())).FirstOrDefault().PawnsListForReading) == null);
        }
    }
}
