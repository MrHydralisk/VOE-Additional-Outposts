using Outposts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts
{
    public class ResultOptionFloat
    {
        public float AmountPerPawn;

        public List<AmountBySkillFloat> AmountsPerSkills;

        public float BaseAmount;

        public List<AmountBySkillFloat> MinSkills;

        public ThingDef Thing;

        public int Amount(List<Pawn> pawns)
        {
            return Mathf.RoundToInt((float)(BaseAmount + AmountPerPawn * pawns.Count + (AmountsPerSkills?.Sum((AmountBySkillFloat x) => x.Amount(pawns)) ?? 0)) * OutpostsMod.Settings.ProductionMultiplier);
        }

        public IEnumerable<Thing> Make(List<Pawn> pawns)
        {
            return Thing.Make(Amount(pawns));
        }

        public string Explain(List<Pawn> pawns)
        {
            return $"{Amount(pawns)}x {Thing.LabelCap}";
        }
    }
}
