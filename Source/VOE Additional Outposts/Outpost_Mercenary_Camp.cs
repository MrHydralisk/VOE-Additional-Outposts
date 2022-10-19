using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Outposts;
using Verse;
using UnityEngine;

namespace VOEAdditionalOutposts
{
    class Outpost_Mercenary_Camp : Outpost
    {
        [PostToSetings("VOEAdditionalOutposts.Settings.InjuryReduceOnMax", PostToSetingsAttribute.DrawMode.Slider, 3f, 1f, 10f, null, null)]
        public float InjuryReduceOnMax = 3f;

        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;

        private OutpistExtension_Choose_Mission ChooseMissionCached;
        protected OutpistExtension_Choose_Mission ChooseMission => ChooseMissionCached ?? (ChooseMissionCached = def.GetModExtension<OutpistExtension_Choose_Mission>());

        private int choiceRisk = 0;

        private ResultMissionOption choiceMission => ChooseMission.ResultMissionOptions.FirstOrDefault((ResultMissionOption rmo) => rmo.RiskLvl == choiceRisk);

        public override void Produce()
        {
            int RewardCount = CalcReward(choiceMission);
            if (RewardCount > 0)
            {
                Deliver(ThingDefOf.Silver.Make(RewardCount));
                Pawn doc = AllPawns.Where((Pawn p) => !p.Dead && !p.Downed && p.RaceProps.Humanlike && !p.skills.GetSkill(SkillDefOf.Medicine).TotallyDisabled).OrderByDescending((Pawn p) => p.skills.GetSkill(SkillDefOf.Medicine).Level).FirstOrDefault();
                Log.Message(doc.Name.ToStringSafe());
                int MinorInjury = 0, MajorInjury = 0, Casualties = 0;
                float InjuryReducePerLvl = 0;
                if (InjuryReduceOnMax > 1)
                {
                    InjuryReducePerLvl = InjuryReduceOnMax / 30;
                }
                foreach (Pawn pawn in CapablePawns)
                {
                    float InjuryReduce = ChooseMission.CombinedSkills.TotalSkillValue(pawn) * InjuryReducePerLvl + 1;
                    if (Rand.Chance(choiceMission.FatalInjuryChance / InjuryReduce))
                    {
                        HealthUtility.DamageUntilDead(pawn);
                        Casualties++;
                    }
                    else if (Rand.Chance(choiceMission.MajorInjuryChance / InjuryReduce))
                    {
                        HealthUtility.DamageUntilDowned(pawn);
                        while (pawn.health.HasHediffsNeedingTend())
                            TendUtility.DoTend(doc, pawn, null);
                        if (pawn.Dead)
                            Casualties++;
                        else
                            MajorInjury++;
                    }
                    else
                    {
                        float InjuryChance = choiceMission.MinorInjuryChance / InjuryReduce;
                        int InjuryCount = Mathf.FloorToInt(InjuryChance);
                        if (Rand.Chance(InjuryChance - InjuryCount))
                        {
                            InjuryCount++;
                        }
                        for (int i = 0; i < InjuryCount; i++)
                        {
                            pawn.TakeDamage(new DamageInfo(HealthUtility.RandomViolenceDamageType(), Rand.Range(5, 15)));
                        }
                        while (pawn.health.HasHediffsNeedingTend())
                            TendUtility.DoTend(doc, pawn, null);
                        if (InjuryCount > 0)
                            if (pawn.Dead)
                                Casualties++;
                            else if (pawn.Downed)
                                MajorInjury++;
                            else
                                MinorInjury++;
                    }
                }
                LetterDef ld = LetterDefOf.NeutralEvent;
                string Text = "";
                if (MinorInjury > 0)
                {
                    Text += "VOEAdditionalOutposts.Letters.MissionResultMinorInjury.Text".Translate(MinorInjury) + "\n";
                }
                if (MajorInjury > 0)
                {
                    Text += "VOEAdditionalOutposts.Letters.MissionResultMajorInjury.Text".Translate(MajorInjury) + "\n";
                }
                if (Casualties > 0)
                {
                    Text += "VOEAdditionalOutposts.Letters.MissionResultCasualties.Text".Translate(Casualties) + "\n";
                    ld = LetterDefOf.NegativeEvent;
                }
                if (Text == "")
                {
                    Text = "VOEAdditionalOutposts.Letters.MissionResultNothing.Text".Translate();
                }
                Find.LetterStack.ReceiveLetter("VOEAdditionalOutposts.Letters.MissionResult.Label".Translate(Name), Text, ld);
            }
        }

        public int CalcReward(ResultMissionOption rmo)
        {
            return (int)((rmo.BaseSilverAmount + CalcTotalSkillValue() * rmo.AmountsPerCombinedSkills) * OutpostsMod.Settings.ProductionMultiplier);
        }

        public float CalcTotalSkillValue()
        {
            return ChooseMission.CombinedSkills.TotalSkillValue(CapablePawns.ToList());
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Append(new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new FloatMenu(ChooseMission.ResultMissionOptions.OrEmpty().Select(delegate (ResultMissionOption rmo)
                    {
                        return (rmo.MinCombinedSkills <= 0 || CalcTotalSkillValue() >= rmo.MinCombinedSkills) ? new FloatMenuOption(ChooseExt.ChooseLabel.Formatted(rmo.RiskLvl.ToString()) + ": " + CalcReward(rmo).ToString(), delegate
                        {
                            choiceRisk = rmo.RiskLvl;
                        }, ThingDefOf.Silver) : new FloatMenuOption(ChooseExt.ChooseLabel.Formatted(rmo.RiskLvl.ToString()) + ": " + "Outposts.SkillTooLow".Translate(rmo.MinCombinedSkills.ToString()), null, ThingDefOf.Silver);
                    })
                    .ToList()));
                },
                defaultLabel = ChooseExt.ChooseLabel.Formatted(choiceMission.RiskLvl.ToString()),
                defaultDesc = ChooseExt.ChooseDesc,
                icon = ThingDefOf.Silver.uiIcon
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref choiceRisk, "choiceRisk");
        }

        protected override bool IsCapable(Pawn pawn)
        {
            if (pawn.Dead)
            {
                return false;
            }
            if (pawn.Downed)
            {
                return false;
            }
            return base.IsCapable(pawn);
        }

        public override string ProductionString()
        {
            if (Ext == null || choiceMission == null)
            {
                return "";
            }
            return "VOEAdditionalOutposts.WillFinishMission".Translate(choiceMission.RiskLvl.ToString(), CalcReward(choiceMission).ToString(), CapablePawns.Count().ToString(), TimeTillProduction).RawText;
        }

        public override string RelevantSkillDisplay()
        {
            return "Outposts.TotalSkill".Translate(ChooseMission.CombinedSkills.CombinedSkillName, CalcTotalSkillValue()).RawText;
        }
    }
}
