using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts
{
    [StaticConstructorOnStartup]
    public class WITab_Outpost_Educational_Center : WITab
    {
        private Vector2 scrollPosition;
        private float scrollViewHeight;
        public Outpost_Educational_Center SelPrison => base.SelObject as Outpost_Educational_Center;

        public WITab_Outpost_Educational_Center()
        {
            size = new Vector2(500f, 500f);
            labelKey = "VOEAdditionalOutposts.EducationTab";
        }

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect outRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
            float curY = 0f;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            DoRows(ref curY, viewRect, outRect);
            scrollViewHeight = curY;
            Widgets.EndScrollView();
        }

        private void DoRows(ref float curY, Rect scrollViewRect, Rect scrollOutRect)
        {
            List<Pawn> teachers = SelPrison.Teachers;
            if (teachers.Count() > 0)
            {
                List<int> teachersBySkills = new List<int>();
                foreach (SkillDef allDef in DefDatabase<SkillDef>.AllDefs)
                {
                    teachersBySkills.Add(0);
                }
                Rect rect = new Rect(0f, curY, scrollViewRect.width, 28f);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.LowerCenter;
                Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 24f) / 2f, 72f, 24f), "VOEAdditionalOutposts.Efficiency".Translate());    
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineVertical(rect.x + rect.width - 74f, rect.y + (rect.height - 24f) / 2f, 24f);
                GUI.color = Color.white;
                rect.width -= 75f;
                Widgets.Label(new Rect(rect.x + rect.width - 107f, rect.y + (rect.height - 24f) / 2f, 107f, 24f), "SkillLevel".Translate());
                rect.width -= 110f;
                Text.Anchor = TextAnchor.LowerLeft;
                Rect rect3 = new Rect(4f, curY, rect.width, rect.height);
                Widgets.Label(rect3, "VOEAdditionalOutposts.Teachers".Translate());
                curY += 37f;
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineHorizontal(0f, curY, scrollViewRect.width);
                curY += 2f;
                foreach (Pawn pawn in teachers)
                {
                    var (skill, skillIndex) = pawn.skills.skills.Select((x, i) => (x, i)).Max();
                    teachersBySkills[skillIndex]++;
                    DoTeachersRow(pawn, scrollViewRect.width, ref curY, skill, 1f / teachersBySkills[skillIndex]);
                }
            }
            List<Pawn> students = SelPrison.Students;
            if (students.Count() > 0)
            {
                Rect rect = new Rect(0f, curY, scrollViewRect.width, 28f);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.LowerCenter;
                Widgets.Label(new Rect(rect.x + rect.width - 140f, rect.y + (rect.height - 24f) / 2f, 140f, 24f), "StatsReport_GrowthTier".Translate());
                rect.width -= 143f;
                Text.Anchor = TextAnchor.LowerLeft;
                GUI.color = new Color(1f, 0.85f, 0.5f);
                Rect rect3 = new Rect(4f, curY, rect.width, rect.height);
                Widgets.Label(rect3, "VOEAdditionalOutposts.Students".Translate());
                curY += 37f;
                Widgets.DrawLineHorizontal(0f, curY, scrollViewRect.width);
                curY += 2f;
                foreach (Pawn pawn in students)
                {
                    DoStudentsRow(pawn, scrollViewRect.width, ref curY);
                }
            }
        }

        protected virtual void DoTeachersRow(Pawn pawn, float width, ref float curY, SkillRecord skill, float efficiency)
        {
            Rect rect = new Rect(0f, curY, width, 28f);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 24f) / 2f, 72f, 24f), efficiency.ToString("P"));
            rect.width -= 75f;
            Rect rect4 = new Rect(rect.x + rect.width - 24f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Widgets.FillableBar(rect4, Mathf.Max(0.01f, (float)skill.Level / 20f), SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.1f)), null, doBorder: false);
            Widgets.Label(rect4, skill.Level.ToString());
            rect.width -= 27f;
            Widgets.Label(new Rect(rect.x + rect.width - 80f, rect.y + (rect.height - 24f) / 2f, 80f, 24f), skill.def.skillLabel.CapitalizeFirst());
            rect.width -= 83f;
            Rect rect2 = new Rect(4f, curY, 28f, 28f);
            Widgets.ThingIcon(rect2, pawn);
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = PawnNameColorUtility.PawnNameColorOf(pawn);
            Rect rect3 = new Rect(36f, curY, rect.width - 36f, rect.height);
            string text2 = pawn.LabelCap;
            Text.WordWrap = false;
            Widgets.Label(rect3, text2.StripTags().Truncate(rect3.width));
            Text.WordWrap = true;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, text2);
            curY += 28f;
        }

        protected virtual void DoStudentsRow(Pawn pawn, float width, ref float curY)
        {
            Rect rect = new Rect(0f, curY, width, 28f);
            bool Recruitable = pawn.guest.Recruitable;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect rectG = new Rect(rect.x + rect.width - 140f, rect.y + (rect.height - 24f) / 2f, 140f, 24f);
            Widgets.FillableBar(rectG, pawn.ageTracker.PercentToNextGrowthTier, SolidColorMaterials.NewSolidColorTexture(new Color(0.1254902f, 46f / 85f, 0f)), SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty), doBorder: true);
            if (Mouse.IsOver(rectG))
            {
                Widgets.DrawHighlight(rect);
                string text = GrowthTierTooltip(pawn, rect, pawn.ageTracker.GrowthTier);
                TooltipHandler.TipRegion(rect, new TipSignal(text, pawn.thingIDNumber ^ 0x31F031E9));
            }
            rect.width -= 143f;
            Rect rect2 = new Rect(4f, curY, 28f, 28f);
            Widgets.ThingIcon(rect2, pawn);
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = PawnNameColorUtility.PawnNameColorOf(pawn);
            Rect rect3 = new Rect(36f, curY, rect.width - 36f, rect.height);
            string text2 = pawn.LabelCap;
            Text.WordWrap = false;
            Widgets.Label(rect3, text2.StripTags().Truncate(rect3.width));
            Text.WordWrap = true;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, text2);
            curY += 28f;
        }

        private string GrowthTierTooltip(Pawn child, Rect rect, int tier)
        {
            TaggedString taggedString = ("StatsReport_GrowthTier".Translate() + ": ").AsTipTitle() + tier + "\n" + "StatsReport_GrowthTierDesc".Translate().Colorize(ColoredText.SubtleGrayColor) + "\n\n";
            if (child.ageTracker.AtMaxGrowthTier)
            {
                taggedString += ("MaxTier".Translate() + ": ").AsTipTitle() + "MaxTierDesc".Translate(child.Named("PAWN"));
            }
            else
            {
                taggedString += ("ProgressToNextGrowthTier".Translate() + ": ").AsTipTitle() + Mathf.FloorToInt(child.ageTracker.growthPoints).ToString() + " / " + GrowthUtility.GrowthTierPointsRequirements[tier + 1];
                if (child.ageTracker.canGainGrowthPoints)
                {
                    taggedString += string.Format(" (+{0})", "PerDay".Translate(child.ageTracker.GrowthPointsPerDay.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
                }
            }
            if (child.ageTracker.AgeBiologicalYears < 13)
            {
                int num = 0;
                for (int i = child.ageTracker.AgeBiologicalYears + 1; i <= 13; i++)
                {
                    if (GrowthUtility.IsGrowthBirthday(i))
                    {
                        num = i;
                        break;
                    }
                }
                taggedString += "\n\n" + ("NextGrowthMomentAt".Translate() + ": ").AsTipTitle() + num;
            }
            taggedString += "\n\n" + ("ThisGrowthTier".Translate(tier) + ":").AsTipTitle();
            if (GrowthUtility.PassionGainsPerTier[tier] > 0)
            {
                taggedString += "\n  - " + "NumPassionsFromOptions".Translate(GrowthUtility.PassionGainsPerTier[tier], GrowthUtility.PassionChoicesPerTier[tier]);
            }
            taggedString += "\n  - " + "NumTraitsFromOptions".Translate(GrowthUtility.TraitGainsPerTier[tier], GrowthUtility.TraitChoicesPerTier[tier]);
            if (!child.ageTracker.AtMaxGrowthTier)
            {
                taggedString += "\n\n" + ("NextGrowthTier".Translate(tier + 1) + ":").AsTipTitle();
                if (GrowthUtility.PassionGainsPerTier[tier + 1] > 0)
                {
                    taggedString += "\n  - " + "NumPassionsFromOptions".Translate(GrowthUtility.PassionGainsPerTier[tier + 1], GrowthUtility.PassionChoicesPerTier[tier + 1]);
                }
                taggedString += "\n  - " + "NumTraitsFromOptions".Translate(GrowthUtility.TraitGainsPerTier[tier + 1], GrowthUtility.TraitChoicesPerTier[tier + 1]);
            }
            return taggedString.Resolve();
        }
    }
}