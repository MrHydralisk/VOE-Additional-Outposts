using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts
{
    [StaticConstructorOnStartup]
    public class WITab_Outpost_Bank : WITab
    {
        private Vector2 scrollPosition;
        private float scrollViewHeight;
        public Outpost_Bank SelBank => base.SelObject as Outpost_Bank;

        public WITab_Outpost_Bank()
        {
            size = new Vector2(432f, 500f);
            labelKey = "VOEAdditionalOutposts.DepositTab";
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
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DoRows(ref float curY, Rect scrollViewRect, Rect scrollOutRect)
        {
            Rect rect = new Rect(0f, curY, scrollViewRect.width, 24f);
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), ThingDefOf.Silver.LabelCap);
            curY += 25f;
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(0f, curY, scrollViewRect.width);
            curY += 2f;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.LowerLeft;
            if (SelBank.currentSilverDuration > 0)
            {
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositCurrent".Translate((Mathf.Pow(SelBank.currentSilverDeposit.InterestPerSeason * SelBank.SilverInterestMultiplier + 1f, SelBank.currentSilverDeposit.DurationInSeasons) - 1f).ToStringPercent(), SelBank.currentSilverDeposit.DurationInSeasons) + ":");
                curY += 25f;
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), SelBank.SilverDeposit.ToString("F0") + " " + ThingDefOf.Silver.LabelCap);
                curY += 25f;
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositSeasonsLeft".Translate(Mathf.Ceil((float)SelBank.ticksTillSilverDepositEnd / SelBank.ticksPerSeason)));
                curY += 25f;
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositTimeLeft".Translate(SelBank.ticksTillSilverDepositEnd.ToStringTicksToPeriodVerbose()));
                curY += 25f;
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositIncome".Translate((SelBank.SilverDeposit * SelBank.currentSilverDeposit.InterestPerSeason * SelBank.SilverInterestMultiplier).ToString("F0") + " " + ThingDefOf.Silver.LabelCap));
                curY += 25f;
                if (Widgets.ButtonText(new Rect(rect.x, curY, rect.width, rect.height), SelBank.SilverMP != null ? "VOEAdditionalOutposts.DepositDeliver".Translate(SelBank.SilverMP) : "VOEAdditionalOutposts.DepositKeep".Translate(), drawBackground: false))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(new FloatMenuOption(SelBank.LabelCap, delegate
                    {
                        SelBank.SilverMP = null;
                    },
                    itemIcon: SelBank.ExpandingIcon,
                    iconColor: SelBank.ExpandingIconColor));
                    foreach (Map map in from m in Find.Maps
                                        where m.IsPlayerHome
                                        orderby Find.WorldGrid.ApproxDistanceInTiles(m.Parent.Tile, SelBank.Tile)
                                        select m)
                    {
                        list.Add(new FloatMenuOption(map.Parent.LabelCap, delegate
                        {
                            SelBank.SilverMP = map.Parent;
                        },
                        itemIcon: map.Parent.ExpandingIcon,
                        iconColor: map.Parent.ExpandingIconColor));
                    }
                    foreach (MapParent mapParent in from m in Find.WorldObjects.AllWorldObjects.OfType<Outpost>()
                                                    where m.Tile != SelBank.Tile
                                                    orderby Find.WorldGrid.ApproxDistanceInTiles(m.Tile, SelBank.Tile)
                                                    select m)
                    {
                        list.Add(new FloatMenuOption(mapParent.LabelCap, delegate
                        {
                            SelBank.SilverMP = mapParent;
                        },
                        itemIcon: mapParent.ExpandingIcon,
                        iconColor: mapParent.ExpandingIconColor));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                curY += 25f;
                if (Widgets.ButtonText(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositStop".Translate()))
                {
                    SelBank.StopSilverDeposit();
                }
                curY += 25f;
            }
            else
            {
                if (Widgets.ButtonText(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositStart".Translate()))
                {
                    SelBank.StartSilverDeposit();
                }
                curY += 25f;
            }
            float rwidth = rect.width - 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            int num1 = SelBank.Things.Sum((Thing t) => t.def == ThingDefOf.Silver ? t.stackCount : 0);
            if (Widgets.ButtonText(new Rect(rect.x + rwidth, curY, 24f, rect.height), "+"))
            {
                Find.WindowStack.Add(new Dialog_Slider(delegate (int x)
                {
                    return x.ToString();
                }, Mathf.Min(1, num1), num1, delegate (int x)
                {
                    SelBank.AddSilver(x);
                }));
            }
            rwidth -= 25f;
            if (Widgets.ButtonText(new Rect(rect.x + rwidth, curY, 24f, rect.height), "-"))
            {
                Find.WindowStack.Add(new Dialog_Slider(delegate (int x)
                {
                    return x.ToString();
                }, Mathf.Min(1, (int)SelBank.SilverToAdd), (int)SelBank.SilverToAdd, delegate (int x)
                {
                    SelBank.RemoveSilver(x);
                }));
            }
            Text.Anchor = TextAnchor.LowerLeft;
            rwidth -= 25f;
            Widgets.Label(new Rect(rect.x, curY, rwidth, rect.height), "VOEAdditionalOutposts.DepositAddedNextSeason".Translate());
            curY += 25f;
            Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), SelBank.SilverToAdd.ToString("F0") + " " + ThingDefOf.Silver.LabelCap);
            curY += 25f;
            Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositSelectDeposit".Translate());
            curY += 25f;
            if (Widgets.ButtonText(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositCurrent".Translate((Mathf.Pow(SelBank.choiceSilverDeposit.InterestPerSeason * SelBank.SilverInterestMultiplier + 1f, SelBank.choiceSilverDeposit.DurationInSeasons) - 1f).ToStringPercent(), SelBank.choiceSilverDeposit.DurationInSeasons)))
            {
                Find.WindowStack.Add(new FloatMenu(SelBank.DepositExt.ResultSilverDepositOptions.Select(delegate (ResultDepositOption rdo)
                {
                    if (SelBank.TotalSkill(SkillDefOf.Social) < rdo.MinSkills)
                        return new FloatMenuOption("VOEAdditionalOutposts.DepositCurrent".Translate((Mathf.Pow(rdo.InterestPerSeason * SelBank.SilverInterestMultiplier + 1f, rdo.DurationInSeasons) - 1f).ToStringPercent(), rdo.DurationInSeasons) + ": " + "Outposts.SkillTooLow".Translate(rdo.MinSkills.ToString()), null);
                    return new FloatMenuOption("VOEAdditionalOutposts.DepositCurrent".Translate((Mathf.Pow(rdo.InterestPerSeason * SelBank.SilverInterestMultiplier + 1f, rdo.DurationInSeasons) - 1f).ToStringPercent(), rdo.DurationInSeasons), delegate
                    {
                        SelBank.choiceSilverDuration = rdo.DurationInSeasons;
                    });
                }).ToList()));
            }
            curY += 25f;
            Widgets.CheckboxLabeled(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositAutoRepeat".Translate(), ref SelBank.SilverDepositRepeat);
            curY += 25f;

            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(0f, curY, scrollViewRect.width);
            curY += 2f;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), ThingDefOf.Gold.LabelCap);
            curY += 25f;
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(0f, curY, scrollViewRect.width);
            curY += 2f;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.LowerLeft;
            if (SelBank.currentGoldDuration > 0)
            {
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositCurrent".Translate((Mathf.Pow(SelBank.currentGoldDeposit.InterestPerSeason * SelBank.GoldInterestMultiplier + 1f, SelBank.currentGoldDeposit.DurationInSeasons) - 1f).ToStringPercent(), SelBank.currentGoldDeposit.DurationInSeasons) + ":");
                curY += 25f;
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), SelBank.GoldDeposit.ToString("F0") + " " + ThingDefOf.Gold.LabelCap);
                curY += 25f;
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositSeasonsLeft".Translate(Mathf.Ceil((float)SelBank.ticksTillGoldDepositEnd / SelBank.ticksPerSeason)));
                curY += 25f;
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositTimeLeft".Translate(SelBank.ticksTillGoldDepositEnd.ToStringTicksToPeriodVerbose()));
                curY += 25f;
                Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositIncome".Translate((SelBank.GoldDeposit * SelBank.currentGoldDeposit.InterestPerSeason * SelBank.GoldInterestMultiplier).ToString("F0") + " " + ThingDefOf.Gold.LabelCap));
                curY += 25f;
                if (Widgets.ButtonText(new Rect(rect.x, curY, rect.width, rect.height), SelBank.GoldMP != null ? "VOEAdditionalOutposts.DepositDeliver".Translate(SelBank.GoldMP) : "VOEAdditionalOutposts.DepositKeep".Translate(), drawBackground: false))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(new FloatMenuOption(SelBank.LabelCap, delegate
                    {
                        SelBank.GoldMP = null;
                    },
                    itemIcon: SelBank.ExpandingIcon,
                    iconColor: SelBank.ExpandingIconColor));
                    foreach (Map map in from m in Find.Maps
                                        where m.IsPlayerHome
                                        orderby Find.WorldGrid.ApproxDistanceInTiles(m.Parent.Tile, SelBank.Tile)
                                        select m)
                    {
                        list.Add(new FloatMenuOption(map.Parent.LabelCap, delegate
                        {
                            SelBank.GoldMP = map.Parent;
                        },
                        itemIcon: map.Parent.ExpandingIcon,
                        iconColor: map.Parent.ExpandingIconColor));
                    }
                    foreach (MapParent mapParent in from m in Find.WorldObjects.AllWorldObjects.OfType<Outpost>()
                                                    where m.Tile != SelBank.Tile
                                                    orderby Find.WorldGrid.ApproxDistanceInTiles(m.Tile, SelBank.Tile)
                                                    select m)
                    {
                        list.Add(new FloatMenuOption(mapParent.LabelCap, delegate
                        {
                            SelBank.GoldMP = mapParent;
                        },
                        itemIcon: mapParent.ExpandingIcon,
                        iconColor: mapParent.ExpandingIconColor));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                curY += 25f;
                if (Widgets.ButtonText(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositStop".Translate()))
                {
                    SelBank.StopGoldDeposit();
                }
                curY += 25f;
            }
            else
            {
                if (Widgets.ButtonText(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositStart".Translate()))
                {
                    SelBank.StartGoldDeposit();
                }
                curY += 25f;
            }
            float rwidth1 = rect.width - 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            int num2 = SelBank.Things.Sum((Thing t) => t.def == ThingDefOf.Gold ? t.stackCount : 0);
            if (Widgets.ButtonText(new Rect(rect.x + rwidth1, curY, 24f, rect.height), "+"))
            {
                Find.WindowStack.Add(new Dialog_Slider(delegate (int x)
                {
                    return x.ToString();
                }, Mathf.Min(1, num2), num2, delegate (int x)
                {
                    SelBank.AddGold(x);
                }));
            }
            rwidth1 -= 25f;
            if (Widgets.ButtonText(new Rect(rect.x + rwidth1, curY, 24f, rect.height), "-"))
            {
                Find.WindowStack.Add(new Dialog_Slider(delegate (int x)
                {
                    return x.ToString();
                }, Mathf.Min(1, (int)SelBank.GoldToAdd), (int)SelBank.GoldToAdd, delegate (int x)
                {
                    SelBank.RemoveGold(x);
                }));
            }
            Text.Anchor = TextAnchor.LowerLeft;
            rwidth1 -= 25f;
            Widgets.Label(new Rect(rect.x, curY, rwidth1, rect.height), "VOEAdditionalOutposts.DepositAddedNextSeason".Translate());
            curY += 25f;
            Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), SelBank.GoldToAdd.ToString("F0") + " " + ThingDefOf.Gold.LabelCap);
            curY += 25f;
            Widgets.Label(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositSelectDeposit".Translate());
            curY += 25f;
            if (Widgets.ButtonText(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositCurrent".Translate((Mathf.Pow(SelBank.choiceGoldDeposit.InterestPerSeason * SelBank.GoldInterestMultiplier + 1f, SelBank.choiceGoldDeposit.DurationInSeasons) - 1f).ToStringPercent(), SelBank.choiceGoldDeposit.DurationInSeasons)))
            {
                Find.WindowStack.Add(new FloatMenu(SelBank.DepositExt.ResultGoldDepositOptions.Select(delegate (ResultDepositOption rdo)
                {
                    if (SelBank.TotalSkill(SkillDefOf.Social) < rdo.MinSkills)
                        return new FloatMenuOption("VOEAdditionalOutposts.DepositCurrent".Translate((Mathf.Pow(rdo.InterestPerSeason * SelBank.GoldInterestMultiplier + 1f, rdo.DurationInSeasons) - 1f).ToStringPercent(), rdo.DurationInSeasons) + ": " + "Outposts.SkillTooLow".Translate(rdo.MinSkills.ToString()), null);
                    return new FloatMenuOption("VOEAdditionalOutposts.DepositCurrent".Translate((Mathf.Pow(rdo.InterestPerSeason * SelBank.GoldInterestMultiplier + 1f, rdo.DurationInSeasons) - 1f).ToStringPercent(), rdo.DurationInSeasons), delegate
                    {
                        SelBank.choiceGoldDuration = rdo.DurationInSeasons;
                    });
                }).ToList()));
            }
            curY += 25f;
            Widgets.CheckboxLabeled(new Rect(rect.x, curY, rect.width, rect.height), "VOEAdditionalOutposts.DepositAutoRepeat".Translate(), ref SelBank.GoldDepositRepeat);
            curY += 25f;
        }
    }
}