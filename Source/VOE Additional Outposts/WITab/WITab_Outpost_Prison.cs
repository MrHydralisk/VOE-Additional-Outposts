using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts
{
    [StaticConstructorOnStartup]
    public class WITab_Outpost_Prison : WITab
    {
        private Vector2 scrollPosition;
        private float scrollViewHeight;
        public Outpost_Prison SelPrison => base.SelObject as Outpost_Prison;

        public WITab_Outpost_Prison()
        {
            size = new Vector2(500f, 500f);
            labelKey = "VOEAdditionalOutposts.PrisonTab";
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
            List<Pawn> wardens = SelPrison.Wardens;
            if (wardens.Count() > 0)
            {
                Rect rect = new Rect(0f, curY, scrollViewRect.width, 36f);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.LowerCenter;
                Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 34f) / 2f, 72f, 34f), "VOEAdditionalOutposts.InteractionInterval".Translate());
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineVertical(rect.x + rect.width - 74f, rect.y + (rect.height - 34f) / 2f, 34f);
                GUI.color = Color.white;
                rect.width -= 75f;
                Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 34f) / 2f, 72f, 34f), StatDefOf.NegotiationAbility.LabelForFullStatListCap);
                rect.width -= 75f;
                Text.Anchor = TextAnchor.LowerLeft;
                Rect rect3 = new Rect(4f, curY, rect.width, rect.height);
                Widgets.Label(rect3, "VOEAdditionalOutposts.Wardens".Translate());
                curY += 37f;
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineHorizontal(0f, curY, scrollViewRect.width);
                curY += 2f;
                foreach (Pawn pawn in wardens)
                {
                    DoWardenRow(pawn, scrollViewRect.width, ref curY);
                }
            }
            List<Pawn> prisoners = SelPrison.Prisoners;
            if (prisoners.Count() > 0)
            {
                Rect rect = new Rect(0f, curY, scrollViewRect.width, 36f);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.LowerCenter;
                Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 34f) / 2f, 72f, 34f), "VOEAdditionalOutposts.InteractionInterval".Translate());
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineVertical(rect.x + rect.width - 74f, rect.y + (rect.height - 34f) / 2f, 34f);
                rect.width -= 75f;
                if (ModsConfig.IdeologyActive)
                {
                    GUI.color = new Color32(222, 192, 22, byte.MaxValue);
                    Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 34f) / 2f, 72f, 34f), "WillLevel".Translate());
                    GUI.color = Widgets.SeparatorLineColor;
                    Widgets.DrawLineVertical(rect.x + rect.width - 74f, rect.y + (rect.height - 34f) / 2f, 34f);
                    GUI.color = Color.white;
                    rect.width -= 75f;
                }
                GUI.color = Color.white;
                Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 34f) / 2f, 72f, 34f), "RecruitmentResistance".Translate());
                rect.width -= 75f;
                Text.Anchor = TextAnchor.LowerLeft;
                GUI.color = new Color(1f, 0.85f, 0.5f);
                Rect rect3 = new Rect(4f, curY, rect.width, rect.height);
                Widgets.Label(rect3, "VOEAdditionalOutposts.Prisoners".Translate());
                curY += 37f;
                Widgets.DrawLineHorizontal(0f, curY, scrollViewRect.width);
                curY += 2f;
                foreach (Pawn pawn in prisoners)
                {
                    DoPrisonerRow(pawn, scrollViewRect.width, ref curY);
                }
            }
        }

        protected virtual void DoWardenRow(Pawn pawn, float width, ref float curY)
        {
            Rect rect = new Rect(0f, curY, width, 28f);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 24f) / 2f, 72f, 24f), Mathf.Max(0, pawn.mindState.lastAssignedInteractTime - Find.TickManager.TicksGame).TicksToSeconds().ToString("F0"));
            rect.width -= 75f;
            Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 24f) / 2f, 72f, 24f), pawn.GetStatValue(StatDefOf.NegotiationAbility).ToString("F2"));
            rect.width -= 75f;
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

        protected virtual void DoPrisonerRow(Pawn pawn, float width, ref float curY)
        {
            Rect rect = new Rect(0f, curY, width, 28f);
            bool Recruitable = pawn.guest.Recruitable;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 24f) / 2f, 72f, 24f), Mathf.Max(0, pawn.mindState.lastAssignedInteractTime - Find.TickManager.TicksGame).TicksToSeconds().ToString("F0"));
            rect.width -= 75f;
            if (ModsConfig.IdeologyActive)
            {
                GUI.color = new Color32(222, 192, 22, byte.MaxValue);
                Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 24f) / 2f, 72f, 24f), pawn.guest.will.ToString("F2"));
                rect.width -= 75f;
            }
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + rect.width - 72f, rect.y + (rect.height - 24f) / 2f, 72f, 24f), pawn.guest.resistance.ToString("F2"));
            rect.width -= 75f;
            if (!Recruitable)
            {
                Rect r = new Rect(rect.x + rect.width - 24f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
                GUI.color = new Color(1f, 1f, 1f, 0.1f);
                GUI.DrawTexture(r, BaseContent.WhiteTex);
                GUI.color = Color.white;
                GUI.DrawTexture(r, ContentFinder<Texture2D>.Get("UI/Icons/UnwaveringlyLoyal"));
                if (Mouse.IsOver(r))
                {
                    Widgets.DrawHighlight(r);
                    TooltipHandler.TipRegion(r, () => "Unrecruitable".Translate().AsTipTitle().CapitalizeFirst() + "\n\n" + "UnrecruitableDesc".Translate(pawn.Named("PAWN")).Resolve(), 15877733);
                }
            }
            rect.width -= 24f;
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
    }
}