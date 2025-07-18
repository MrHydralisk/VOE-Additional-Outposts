using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VOEAdditionalOutposts
{
    public class AmountByCombinedSkills
    {
        public string CombinedSkillName;

        public List<SkillDef> Skills;

        public List<float> SkillsWeight;

        public bool ImportanceBySkillLvl;

        public float SlaveSkillMultiplier = 0.8f;

        public float PrisonerSkillMultiplier = 0.5f;

        public float TotalSkillValue(List<Pawn> pawns)
        {
            float sum = 0;
            foreach (Pawn pawn in pawns)
            {
                sum += TotalSkillValue(pawn);
            }
            return sum;
        }

        public float TotalSkillValue(Pawn pawn)
        {
            if (ImportanceBySkillLvl)
            {
                Skills.SortByDescending((SkillDef w) => pawn.skills.GetSkill(w).Level);
            }
            float sum = 0;
            for (int i = 0; i < Skills.Count(); i++)
            {
                sum += pawn.skills.GetSkill(Skills[i]).Level * SkillsWeight[i];
            }
            if (pawn.IsPrisoner)
            {
                sum *= PrisonerSkillMultiplier;
            }
            else if (pawn.IsSlave)
            {
                sum *= SlaveSkillMultiplier;
            }
            return sum;
        }
    }
}
