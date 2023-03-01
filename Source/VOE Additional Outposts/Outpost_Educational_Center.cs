#if v1_4
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Outposts;
using Verse;

namespace VOEAdditionalOutposts
{
    public class Outpost_Educational_Center : Outpost
    {
        [PostToSetings("VOEAdditionalOutposts.Settings.TeacherSkillMultiplier", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 5f, null, null)]
        public float TeacherSkillMultiplier = 1f;

        [PostToSetings("VOEAdditionalOutposts.Settings.StudentGrowthMultiplier", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 5f, null, null)]
        public float StudentGrowthMultiplier = 1f;

        [PostToSetings("VOEAdditionalOutposts.Settings.StudentLearningMultiplier", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 5f, null, null)]
        public float StudentLearningMultiplier = 1f;

        private List<Pawn> students => base.AllPawns.Where((Pawn p) => !p.Dead && p.RaceProps.Humanlike && Pawn_AgeTracker.MinGrowthBirthday <= p.ageTracker.AgeBiologicalYearsFloat && p.ageTracker.AgeBiologicalYearsFloat <= Pawn_AgeTracker.MaxGrowthBirthday).ToList();
        private List<Pawn> teachers => base.AllPawns.Where((Pawn p) => !p.Dead && p.RaceProps.Humanlike && p.ageTracker.Adult).ToList();

        public override void Produce()
        {
            if (students.Any())
            {
                float totalSkill = TotalSkill();
                totalSkill /= 180f;
                foreach (Pawn pawn in students)
                {
                    pawn.ageTracker.growthPoints += totalSkill * (pawn.ageTracker.AgeBiologicalYears < 7f ? 0.75f : 1f) * pawn.ageTracker.ChildAgingMultiplier * StudentGrowthMultiplier;
                    if (pawn.skills != null && pawn.skills.skills.Where((SkillRecord x) => !x.TotallyDisabled).TryRandomElement(out var result))
                    {
                        result.Learn(totalSkill * 4800f * StudentLearningMultiplier, direct: true);
                    }
                }
            }
        }

        public float TotalSkill()
        {
            List<int> teachersBySkills = new List<int>();
            float totalSkill = 0;
            foreach (SkillDef allDef in DefDatabase<SkillDef>.AllDefs)
            {
                teachersBySkills.Add(0);
            }
            foreach (Pawn pawn in teachers)
            {
                var (skill, skillIndex) = pawn.skills.skills.Select((x, i) => (x, i)).Max();
                teachersBySkills[skillIndex]++;
                float skillValue = skill.Level;
                if (teachersBySkills[skillIndex] > 1)
                    skillValue /= teachersBySkills[skillIndex];
                totalSkill += skillValue;
            }
            return totalSkill * TeacherSkillMultiplier;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    action = delegate
                    {
                        foreach (Pawn p in CapablePawns)
                        {
                            if (p.ageTracker.AgeBiologicalYearsFloat <= Pawn_AgeTracker.MaxGrowthBirthday)
                            {
                                p.ageTracker.DebugSetAge((long)(p.ageTracker.AgeBiologicalYears + 1) * 3600000);
                            }
                        }
                    },
                    defaultLabel = "Dev: Age students +1 year",
                    defaultDesc = "Will make all students older by 1 year"
                };
                yield return new Command_Action
                {
                    action = delegate
                    {
                        string s = "";
                        foreach (Pawn p in CapablePawns)
                        {
                            if (Pawn_AgeTracker.MinGrowthBirthday <= p.ageTracker.AgeBiologicalYearsFloat && p.ageTracker.AgeBiologicalYearsFloat <= Pawn_AgeTracker.MaxGrowthBirthday)
                            {
                                s += p.Name + ": " + p.ageTracker.GrowthTier.ToStringSafe() + " (" + p.ageTracker.growthPoints.ToStringSafe() + ") +" + p.ageTracker.GrowthPointsPerDay.ToStringSafe() + "\n";
                            }
                        }
                        Log.Message(s);
                    },
                    defaultLabel = "Dev: Log Growth",
                    defaultDesc = "Log Growth Tier, Points and Points per Day"
                };
            }
        }

        public override string ProductionString()
        {
            string productionString = "";
            productionString += "VOEAdditionalOutposts.WillTeach".Translate(students.Count(), teachers.Count(), (TotalSkill() / 180f).ToString("F2"), ((int)TotalSkill()).ToStringSafe(), TimeTillProduction).RawText;
            productionString += base.ProductionString();
            return productionString;
        }
    }
}
#endif