using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Outposts;
using Verse;
using UnityEngine;

namespace VOEAdditionalOutposts
{
    public class Outpost_Embassy : Outpost
    {

        [PostToSetings("VOEAdditionalOutposts.Settings.NegotiationDifficulty", PostToSetingsAttribute.DrawMode.IntSlider, 3, 1, 10, null, null)]
        public int NegotiationDifficulty = 3;

        [PostToSetings("VOEAdditionalOutposts.Settings.PerSocial", PostToSetingsAttribute.DrawMode.Percentage, 0.5f, 0.01f, 2f, null, null)]
        public float PerSocial = 0.5f;

        [PostToSetings("VOEAdditionalOutposts.Settings.PerPawn", PostToSetingsAttribute.DrawMode.Percentage, 0.05f, 0.01f, 1f, null, null)]
        public float PerPawn = 0.05f;
        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;

        private Faction choiceFaction;

        public override void Produce()
        {
            Negotiation(choiceFaction);
        }

        public void Negotiation(Faction targetFaction)
        {
            Faction.OfPlayer.TryAffectGoodwillWith(targetFaction, NegotiationGoodwill(targetFaction), reason: HistoryEventDefOfLocal.EmbassyPeaceTalksSuccess);
        }

        public int NegotiationGoodwill(Faction targetFaction)
        {
            float goodwill = Goodwill();
            if (NegotiationDifficulty > 1)
            {
                goodwill = (goodwill * 100) / ((float)Mathf.Abs(targetFaction.PlayerGoodwill) * (NegotiationDifficulty - 1) + 100f);
            }
            return Mathf.RoundToInt(goodwill);
        }

        public float Goodwill()
        {
            List<Pawn> pawns = CapablePawns.ToList();
            return PerSocial * pawns.Sum((Pawn p) => p.skills.GetSkill(SkillDefOf.Social).Level) * (1f + PerPawn * pawns.Count) * OutpostsMod.Settings.ProductionMultiplier;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Append(new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new FloatMenu(GetExtraOptions().Select(delegate (Faction f)
                    {
                        return new FloatMenuOption("VOEAdditionalOutposts.NegotiateWith".Translate(NegotiationGoodwill(f).ToString(), f.Name).RawText, delegate
                        {
                            choiceFaction = f;
                        }, itemIcon: f.def.FactionIcon, iconColor: f.def.DefaultColor);
                    })
                        .ToList()));
                },
                defaultLabel = ChooseExt.ChooseLabel.Formatted(choiceFaction.Name),
                defaultDesc = ChooseExt.ChooseDesc,
                icon = choiceFaction.def.FactionIcon,
                defaultIconColor = choiceFaction.def.DefaultColor
            });
        }

        public override void RecachePawnTraits()
        {
            base.RecachePawnTraits();
            if (choiceFaction == null)
            {
                choiceFaction = Find.FactionManager.AllFactionsVisibleInViewOrder.Where((Faction f) => !f.temporary && !f.IsPlayer).Where((Faction f) => f.CanEverGiveGoodwillRewards).FirstOrDefault();
            }
        }

        public virtual IEnumerable<Faction> GetExtraOptions()
        {
            return Find.FactionManager.AllFactionsVisibleInViewOrder.Where((Faction f) => !f.temporary && !f.IsPlayer).Where((Faction f) => f.CanEverGiveGoodwillRewards);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref choiceFaction, "choiceFaction");
        }

        public override string ProductionString()
        {
            if (Ext == null || choiceFaction == null)
            {
                return "";
            }
            return "VOEAdditionalOutposts.WillNegotiate".Translate(NegotiationGoodwill(choiceFaction), choiceFaction.Name, TimeTillProduction).RawText;
        }
    }
}