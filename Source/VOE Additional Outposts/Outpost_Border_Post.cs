using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Outposts;
using Verse;
using UnityEngine;

namespace VOEAdditionalOutposts
{
    public class Outpost_Border_Post : Outpost
    {

        [PostToSetings("VOEAdditionalOutposts.Settings.Fine", PostToSetingsAttribute.DrawMode.IntSlider, 1000, 250, 3000, null, null)]
        public int BaseFine = 1000;

        [PostToSetings("VOEAdditionalOutposts.Settings.Melee", PostToSetingsAttribute.DrawMode.Slider, 1.25f, 0.25f, 2.5f, null, null)]
        public float PerMelee = 1.25f;

        [PostToSetings("VOEAdditionalOutposts.Settings.PerSocial", PostToSetingsAttribute.DrawMode.Slider, 1.5f, 0.25f, 2.5f, null, null)]
        public float PerSocial = 1.5f;

        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;

        private string choiceType = "Fine";

        public override void Produce()
        {
            switch (choiceType)
            {
                case("Fine"):
                    {
                        int FineSum = 0;
                        foreach (Pawn p in CapablePawns.ToList())
                        {
                            if (TryCatch(p))
                            {
                                FineSum += FineSilver(p);
                            }
                        }
                        if (FineSum > 0)
                            Deliver(ThingDefOf.Silver.Make(FineSum));
                        else
                            Find.LetterStack.ReceiveLetter("VOEAdditionalOutposts.Letters.PatrolFail.Label".Translate(Name), "VOEAdditionalOutposts.Letters.PatrolFail.Text".Translate(Name), LetterDefOf.NeutralEvent);
                        break;
                    }
                case("Imprison"):
                    {
                        List<Pawn> Prisoners = new List<Pawn>();
                        List<Faction> hostileFactions = Find.FactionManager.AllFactionsVisibleInViewOrder.Where((Faction f) => !f.temporary && !f.IsPlayer && f.PlayerRelationKind == FactionRelationKind.Hostile && f.def.humanlikeFaction).ToList();
                        if (hostileFactions.EnumerableNullOrEmpty())
                        {
                            Log.Message("Hostile factions not found");
                        }
                        else
                        {
                            foreach (Pawn p in CapablePawns.ToList())
                            {
                                if (TryCatch(p))
                                {
                                    Pawn prisoner = PawnGenerator.GeneratePawn(PawnKindDefOf.Villager, hostileFactions.ElementAt(Rand.Range(0, hostileFactions.Count())));
                                    prisoner.equipment.DestroyAllEquipment();
                                    prisoner.inventory.DestroyAll();
                                    prisoner.apparel.WornApparel.RemoveAll((Apparel a) => a.MarketValue > 100f);
                                    if (p.skills.GetSkill(SkillDefOf.Social).Level < 4)
                                    {
                                        HealthUtility.DamageLegsUntilIncapableOfMoving(prisoner);
                                        while (prisoner.health.HasHediffsNeedingTend())
                                            TendUtility.DoTend(p, prisoner, null);
                                    }
                                    else if (p.skills.GetSkill(SkillDefOf.Social).Level < 10)
                                    {
                                        int AtkAmount = Rand.Range(1, Mathf.CeilToInt((10f - p.skills.GetSkill(SkillDefOf.Social).Level) / 2) + 1);
                                        for (int i = 0; i < AtkAmount; i++)
                                            prisoner.TakeDamage(new DamageInfo(HealthUtility.RandomViolenceDamageType(), Rand.Range(5, 10)));
                                        while (prisoner.health.HasHediffsNeedingTend())
                                            TendUtility.DoTend(p, prisoner, null);
                                    }
                                    prisoner.Faction.Notify_MemberCaptured(prisoner, Faction.OfPlayer);
                                    prisoner.guest.CapturedBy(Faction.OfPlayer, p);
                                    Prisoners.Add(prisoner);
                                }
                            }
                        }
                        if (Prisoners.Count() > 0)
                            Deliver(Prisoners);
                        else
                            Find.LetterStack.ReceiveLetter("VOEAdditionalOutposts.Letters.PatrolFail.Label".Translate(Name), "VOEAdditionalOutposts.Letters.PatrolFail.Text".Translate(Name), LetterDefOf.NeutralEvent);
                        break;
                    }
                default:
                    {
                        Log.Error("Product type not chosen");
                        break;
                    }
            }
        }

        public int FineSilver(Pawn p)
        {
            return (int)(BaseFine * (1f + PerSocial * p.skills.GetSkill(SkillDefOf.Social).Level / 100f) * OutpostsMod.Settings.ProductionMultiplier);
        }

        public bool TryCatch(Pawn p)
        {
            return Rand.Chance((float)p.skills.GetSkill(SkillDefOf.Melee).Level * PerMelee / 100f);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Append(new Command_Action
            {
                action = delegate
                {
                    List<FloatMenuOption> FMO = new List<FloatMenuOption>();
                    FMO.Add(new FloatMenuOption("VOEAdditionalOutposts.PatrolFine".Translate().RawText, delegate
                    {
                        choiceType = "Fine";
                    }, ThingDefOf.Silver));
                    FMO.Add(new FloatMenuOption("VOEAdditionalOutposts.PatrolImprison".Translate().RawText, delegate
                    {
                        choiceType = "Imprison";
                    }, ContentFinder<Texture2D>.Get("Icons/BorderPostImprison"), Color.white));
                    Find.WindowStack.Add(new FloatMenu(FMO));
                },
                defaultLabel = ChooseExt.ChooseLabel.Formatted(choiceType),
                defaultDesc = ChooseExt.ChooseDesc,
                icon = choiceType == "Fine" ? ThingDefOf.Silver.uiIcon : TexOutposts.ImprisonTex
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref choiceType, "choiceType");
        }
        
        public override string ProductionString()
        {
            if (Ext == null || choiceType == null)
            {
                return "";
            }
            return "VOEAdditionalOutposts.WillPatrol".Translate(choiceType == "Fine" ? "VOEAdditionalOutposts.PatrolFine".Translate().RawText : "VOEAdditionalOutposts.PatrolImprison".Translate().RawText, TimeTillProduction).RawText;
        }

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns)
        {
            return (Find.WorldGrid[tile].Roads.NullOrEmpty()) ? "VOEAdditionalOutposts.MustBeMade.Road".Translate() : (Find.WorldObjects.AllWorldObjects.OfType<Outpost_Border_Post>().Count((Outpost_Border_Post s) => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, tile) < 6f) > 0)? "VOEAdditionalOutposts.MustBeMade.FarFromSame".Translate(6) : ((TaggedString)null);
        }

        public static string RequirementsString(int tile, List<Pawn> pawns)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("VOEAdditionalOutposts.MustBeMade.Road".Translate().Requirement(Find.WorldGrid[tile].Roads.NullOrEmpty()));
            stringBuilder.AppendLine("VOEAdditionalOutposts.MustBeMade.FarFromSame".Translate(6).Requirement(Find.WorldObjects.AllWorldObjects.OfType<Outpost_Border_Post>().Count((Outpost_Border_Post s) => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, tile) < 6f) > 0));
            return stringBuilder.ToString();
        }
    }
}
