using Outposts;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VOEAdditionalOutposts
{
    public class Outpost_ChooseResultFloat : Outpost
    {
        private ThingDef choice;

        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;

        private OutpostExtension_ChooseFloat extensionCached;
        protected OutpostExtension_ChooseFloat ChooseExtFloat => extensionCached ?? (extensionCached = def.GetModExtension<OutpostExtension_ChooseFloat>());

        private ResultOptionFloat ResultOptionFloat => ChooseExtFloat.ResultOptions.FirstOrDefault((ResultOptionFloat rof) => rof.Thing == choice);

        public override IEnumerable<Thing> ProducedThings()
        {
            return ResultOptionFloat.Make(CapablePawns.ToList());
        }

        public override void RecachePawnTraits()
        {
            base.RecachePawnTraits();
            if (choice == null)
            {
                choice = ChooseExtFloat.ResultOptions.FirstOrDefault().Thing;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new FloatMenu(ChooseExtFloat.ResultOptions.Select(delegate (ResultOptionFloat rof)
                    {
                        List<AmountBySkillFloat> minSkills = rof.MinSkills;
                        if (!rof.Thing.IsResearchFinished)
                        {
                            return new FloatMenuOption("VOEPowerGrid.ResearchNotFinished".Translate().RawText, action: null, shownItemForIcon: rof.Thing);
                        }
                        else if (minSkills == null || minSkills.All((AmountBySkillFloat absf) => base.CapablePawns.Sum((Pawn p) => p.skills.GetSkill(absf.Skill).Level) >= absf.Count))
                        {
                            return new FloatMenuOption(rof.Explain(base.CapablePawns.ToList()), delegate
                            {
                                choice = rof.Thing;
                            }, shownItemForIcon: rof.Thing);
                        }
                        else
                        {
                            return new FloatMenuOption(rof.Explain(base.CapablePawns.ToList()) + " - " + "Outposts.SkillTooLow".Translate(rof.MinSkills.Max((AmountBySkillFloat abs) => abs.Count)), action: null, shownItemForIcon: rof.Thing);
                        }
                    })
                        .ToList()));
                },
                defaultLabel = ChooseExt.ChooseLabel,
                defaultDesc = ChooseExt.ChooseDesc,
                icon = choice.uiIcon
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref choice, "choice");
        }

        public override string ProductionString()
        {
            if (Ext == null || choice == null)
            {
                return "";
            }
            return "Outposts.WillProduce.1".Translate(ResultOptionFloat.Amount(CapablePawns.ToList()), ResultOptionFloat.Thing.label, TimeTillProduction).RawText;
        }

        public override string RelevantSkillDisplay()
        {
            List<string> Display = new List<string>();
            Display.Add(base.RelevantSkillDisplay());
            Display.AddRange(ChooseExtFloat.AdditionalDisplaySkills.Select((SkillDef skill) => "Outposts.TotalSkill".Translate(skill.skillLabel, TotalSkill(skill)).RawText));
            return string.Join("\n", Display);
        }
    }
}