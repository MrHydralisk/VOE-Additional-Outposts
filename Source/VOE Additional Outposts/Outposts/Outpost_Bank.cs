using Outposts;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts
{
    public class Outpost_Bank : Outpost
    {
        [PostToSetings("VOEAdditionalOutposts.Settings.DepositWealthPerSocial", PostToSetingsAttribute.DrawMode.IntSlider, 500, 100, 2000, null, null)]
        public int PerSocial = 500;

        [PostToSetings("VOEAdditionalOutposts.Settings.SilverInterestMultiplier", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.25f, 4f, null, null)]
        public float SilverInterestMultiplier = 1f;

        [PostToSetings("VOEAdditionalOutposts.Settings.GoldInterestMultiplier", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.25f, 4f, null, null)]
        public float GoldInterestMultiplier = 1f;

        [PostToSetings("VOEAdditionalOutposts.Settings.EarlyDepositStop", PostToSetingsAttribute.DrawMode.Checkbox, false, 0f, 0f, null, null)]
        public bool isEarlyDepositStopAvailable = false;

        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;
        public OutpistExtension_Choose_Deposit DepositExt => DepositExtCached ?? (DepositExtCached = def.GetModExtension<OutpistExtension_Choose_Deposit>());

        private OutpistExtension_Choose_Deposit DepositExtCached;

        public int ticksPerSeason => 900000;
        public int MaxDepositWealth => TotalSkill(SkillDefOf.Social) * PerSocial;

        public float SilverDeposit;
        public float SilverToAdd;
        public int ticksTillSilverDepositEnd;
        public int choiceSilverDuration = 1;
        public ResultDepositOption choiceSilverDeposit => DepositExt.ResultSilverDepositOptions.FirstOrDefault((ResultDepositOption rdo) => rdo.DurationInSeasons == choiceSilverDuration);
        public int currentSilverDuration = -1;
        public ResultDepositOption currentSilverDeposit => DepositExt.ResultSilverDepositOptions.FirstOrDefault((ResultDepositOption rdo) => rdo.DurationInSeasons == currentSilverDuration);
        public bool SilverDepositRepeat;
        public MapParent SilverMP;

        public float GoldDeposit;
        public float GoldToAdd;
        public int ticksTillGoldDepositEnd;
        public int choiceGoldDuration = 1;
        public ResultDepositOption choiceGoldDeposit => DepositExt.ResultGoldDepositOptions.FirstOrDefault((ResultDepositOption rdo) => rdo.DurationInSeasons == choiceGoldDuration);
        public int currentGoldDuration = -1;
        public ResultDepositOption currentGoldDeposit => DepositExt.ResultGoldDepositOptions.FirstOrDefault((ResultDepositOption rdo) => rdo.DurationInSeasons == currentGoldDuration);
        public bool GoldDepositRepeat;
        public MapParent GoldMP;

        public override void Produce()
        {
        }

        public void ProduceSilverInterest(bool last = false)
        {
            float Silver = SilverDeposit * currentSilverDeposit.InterestPerSeason * SilverInterestMultiplier;
            if (Silver > 0)
            {
                Messages.Message("VOEAdditionalOutposts.BankInterestMsg".Translate(Name, ThingDefOf.Silver.label, Silver.ToString("F0")), new LookTargets(this), MessageTypeDefOf.PositiveEvent);
                if (SilverMP == null)
                {
                    SilverDeposit += CanAddToSilverDeposit(Silver);
                }
                else
                {
                    DeliverCustom(ThingDefOf.Silver.Make((int)Silver), SilverMP);
                }
            }
            if (last)
            {
                if (SilverDepositRepeat)
                {
                    SilverToAdd += SilverDeposit;
                    SilverDeposit = 0;
                }
                else
                {
                    StopSilverDeposit();
                }
            }
        }
        public void ProduceGoldInterest(bool last = false)
        {
            float Gold = GoldDeposit * currentGoldDeposit.InterestPerSeason * GoldInterestMultiplier;
            if (Gold > 0)
            {
                Messages.Message("VOEAdditionalOutposts.BankInterestMsg".Translate(Name, ThingDefOf.Gold.label, Gold.ToString("F0")), new LookTargets(this), MessageTypeDefOf.PositiveEvent);
                if (GoldMP == null)
                {
                    GoldDeposit += CanAddToGoldDeposit(Gold);
                }
                else
                {
                    DeliverCustom(ThingDefOf.Gold.Make((int)Gold), GoldMP);
                }
            }
            if (last)
            {
                if (GoldDepositRepeat)
                {
                    GoldToAdd += GoldDeposit;
                    GoldDeposit = 0;
                }
                else
                {
                    StopGoldDeposit();
                }
            }
        }

        public float CanAddToSilverDeposit(float amount)
        {
            float canAdd = Mathf.Min(amount, MaxDepositWealth - SilverDeposit);
            int extra = (int)(amount - canAdd);
            if (extra > 0)
            {
                foreach (Thing t in ThingDefOf.Silver.Make(extra))
                {
                    AddItem(t);
                }
            }
            return canAdd;
        }
        public float CanAddToGoldDeposit(float amount)
        {
            float canAdd = Mathf.Min(amount, MaxDepositWealth / 10 - GoldDeposit);
            int extra = (int)(amount - canAdd);
            if (extra > 0)
            {
                foreach (Thing t in ThingDefOf.Gold.Make(extra))
                {
                    AddItem(t);
                }
            }
            return canAdd;
        }

        public void DeliverCustom(IEnumerable<Thing> items, MapParent mapParent)
        {
            if (items.Count() > 0 && items.Any(t => t.stackCount > 0))
            {
                if (mapParent is Outpost outpost)
                {
                    ThingDefCountClass tdcc = new ThingDefCountClass(items.FirstOrDefault().def, 0);
                    foreach (Thing t in items)
                    {
                        tdcc.count += t.stackCount;
                        outpost.AddItem(t);
                    }
                    TaggedString text = "Outposts.Letters.Items.Text".Translate(Name) + "\n";
                    text += "  - " + tdcc.LabelCap + "\n";
                    Find.LetterStack.ReceiveLetter("Outposts.Letters.Items.Label".Translate(Name), text, LetterDefOf.PositiveEvent, new LookTargets(outpost));
                }
                else
                {
                    Map temp = deliveryMap;
                    deliveryMap = mapParent.Map;
                    Deliver(items);
                    deliveryMap = temp;
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (ticksTillSilverDepositEnd > 0)
            {
                ticksTillSilverDepositEnd--;
                if (ticksTillSilverDepositEnd % ticksPerSeason == 0)
                {
                    ProduceSilverInterest(ticksTillSilverDepositEnd <= 0);
                    if (ticksTillSilverDepositEnd <= 0)
                    {
                        if (SilverDepositRepeat)
                        {
                            StartSilverDeposit();
                        }
                    }
                    else
                    {
                        SilverDeposit += CanAddToSilverDeposit(SilverToAdd);
                        SilverToAdd = 0;
                    }
                }
            }
            if (ticksTillGoldDepositEnd > 0)
            {
                ticksTillGoldDepositEnd--;
                if (ticksTillGoldDepositEnd % ticksPerSeason == 0)
                {
                    ProduceGoldInterest(ticksTillGoldDepositEnd <= 0);
                    if (ticksTillGoldDepositEnd <= 0)
                    {
                        if (GoldDepositRepeat)
                        {
                            StartGoldDeposit();
                        }
                    }
                    else
                    {
                        GoldDeposit += CanAddToGoldDeposit(GoldToAdd);
                        GoldToAdd = 0;
                    }
                }
            }
        }

        public void StartSilverDeposit()
        {
            if ((int)SilverToAdd > 0)
            {
                ticksTillSilverDepositEnd = choiceSilverDeposit.DurationInSeasons * ticksPerSeason;
                SilverDeposit = CanAddToSilverDeposit(SilverToAdd);
                SilverToAdd = 0;
                currentSilverDuration = choiceSilverDuration;
            }
        }
        public void StartGoldDeposit()
        {
            if ((int)GoldToAdd > 0)
            {
                ticksTillGoldDepositEnd = choiceGoldDeposit.DurationInSeasons * ticksPerSeason;
                GoldDeposit = GoldToAdd;
                GoldToAdd = 0;
                currentGoldDuration = choiceGoldDuration;
            }
        }

        public void StopSilverDeposit()
        {
            ticksTillSilverDepositEnd = -1;
            foreach (Thing t in ThingDefOf.Silver.Make((int)SilverDeposit))
            {
                AddItem(t);
            }
            SilverDeposit = 0;
            currentSilverDuration = -1;
        }
        public void StopGoldDeposit()
        {
            ticksTillGoldDepositEnd = -1;
            foreach (Thing t in ThingDefOf.Gold.Make((int)GoldDeposit))
            {
                AddItem(t);
            }
            GoldDeposit = 0;
            currentGoldDuration = -1;
        }

        public void AddSilver(int amount)
        {
            if (amount > 0)
            {
                List<Thing> silverAll = Things.Where((Thing t) => t.def == ThingDefOf.Silver).ToList();
                ThingDefCountClass tdcc = new ThingDefCountClass(ThingDefOf.Silver, silverAll.Sum((Thing t) => t.stackCount));
                if (tdcc.count > 0)
                {
                    int silverMax = Mathf.Min(tdcc.count, amount);
                    TakeItems(ThingDefOf.Silver, silverMax);
                    SilverToAdd += silverMax;
                }
            }
        }
        public void AddGold(int amount)
        {
            if (amount > 0)
            {
                List<Thing> silverAll = Things.Where((Thing t) => t.def == ThingDefOf.Gold).ToList();
                ThingDefCountClass tdcc = new ThingDefCountClass(ThingDefOf.Gold, silverAll.Sum((Thing t) => t.stackCount));
                if (tdcc.count > 0)
                {
                    int silverMax = Mathf.Min(tdcc.count, amount);
                    TakeItems(ThingDefOf.Gold, silverMax);
                    GoldToAdd += silverMax;
                }
            }
        }

        public void RemoveSilver(int amount)
        {
            if (amount > 0)
            {
                float silverMax = Mathf.Min(SilverToAdd, amount);
                if (silverMax > 0)
                {
                    SilverToAdd -= Mathf.Max(silverMax, 0);
                    foreach (Thing t in ThingDefOf.Silver.Make((int)silverMax))
                    {
                        AddItem(t);
                    }
                }
            }
        }
        public void RemoveGold(int amount)
        {
            if (amount > 0)
            {
                float silverMax = Mathf.Min(GoldToAdd, amount);
                if (silverMax > 0)
                {
                    GoldToAdd -= Mathf.Max(silverMax, 0);
                    foreach (Thing t in ThingDefOf.Gold.Make((int)silverMax))
                    {
                        AddItem(t);
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (gizmo is Command command && command.defaultLabel == "Outposts.Commands.DeliveryColony.Label".Translate())
                {

                }
                else
                {
                    yield return gizmo;
                }
            }
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        FloatMenuOption fmo1 = new FloatMenuOption("Silver", delegate
                        {
                            ticksTillSilverDepositEnd = 1;
                        }, ThingDefOf.Silver);
                        options.Add(fmo1);

                        FloatMenuOption fmo2 = new FloatMenuOption("Gold", delegate
                        {
                            ticksTillGoldDepositEnd = 1;
                        }, ThingDefOf.Gold);
                        options.Add(fmo2);

                        Find.WindowStack.Add(new FloatMenu(options));
                    },
                    defaultLabel = "Dev: 0 the timers",
                    defaultDesc = "Skip the timer"
                };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SilverDeposit, "SilverDeposit");
            Scribe_Values.Look(ref SilverToAdd, "SilverToAdd");
            Scribe_Values.Look(ref ticksTillSilverDepositEnd, "ticksTillSilverDepositEnd");
            Scribe_Values.Look(ref choiceSilverDuration, "choiceSilverDuration");
            Scribe_Values.Look(ref currentSilverDuration, "currentSilverDuration");
            Scribe_Values.Look(ref SilverDepositRepeat, "SilverDepositRepeat");
            Scribe_References.Look(ref SilverMP, "SilverMP");

            Scribe_Values.Look(ref GoldDeposit, "GoldDeposit");
            Scribe_Values.Look(ref GoldToAdd, "GoldToAdd");
            Scribe_Values.Look(ref ticksTillGoldDepositEnd, "ticksTillGoldDepositEnd");
            Scribe_Values.Look(ref choiceGoldDuration, "choiceGoldDuration");
            Scribe_Values.Look(ref currentGoldDuration, "currentGoldDuration");
            Scribe_Values.Look(ref GoldDepositRepeat, "GoldDepositRepeat");
            Scribe_References.Look(ref GoldMP, "GoldMP");
        }

        public override string ProductionString()
        {
            return "VOEAdditionalOutposts.DepositMaxWealth".Translate(MaxDepositWealth).RawText;
        }
    }
}