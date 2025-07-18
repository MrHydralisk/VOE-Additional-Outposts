﻿using Outposts;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts
{
    public class Outpost_Prison : Outpost
    {
        [PostToSetings("VOEAdditionalOutposts.Settings.PerSocial", PostToSetingsAttribute.DrawMode.IntSlider, 20, 10, 100, null, null)]
        public int PerSocial = 20;

        [PostToSetings("VOEAdditionalOutposts.Settings.PerPrisonerSkill", PostToSetingsAttribute.DrawMode.IntSlider, 35, 10, 100, null, null)]
        public int PerPrisonerSkill = 35;

        [PostToSetings("VOEAdditionalOutposts.Settings.InteractionIntervalReductionPerWarden", PostToSetingsAttribute.DrawMode.Percentage, 0.2f, 0, 0.5f, null, null)]
        public float InteractionIntervalReductionPerWarden = 0.2f;

        [PostToSetings("VOEAdditionalOutposts.Settings.MaxInteractionInterval", PostToSetingsAttribute.DrawMode.IntSlider, 30000, 15000, 60000, null, null)]
        public int MaxInteractionInterval = 30000;

        [PostToSetings("VOEAdditionalOutposts.Settings.MinInteractionInterval", PostToSetingsAttribute.DrawMode.IntSlider, 10000, 5000, 60000, null, null)]
        public int MinInteractionInterval = 10000;
        private int TicksPerHemogen = 80000;
        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;

        private List<Pawn> prisoners => base.AllPawns.Where((Pawn p) => !p.Dead && p.RaceProps.Humanlike && p.IsPrisoner).OrderBy((Pawn p) => p.guest.resistance).ToList();
        public List<Pawn> Prisoners => prisoners;

        private List<Pawn> wardens => base.AllPawns.Where((Pawn p) => !p.Dead && p.RaceProps.Humanlike && !p.IsPrisoner && !StatDefOf.NegotiationAbility.Worker.IsDisabledFor(p)).OrderByDescending((Pawn p) => p.GetStatValue(StatDefOf.NegotiationAbility)).ToList();
        public List<Pawn> Wardens => wardens;

        private string choiceType = "Recruit";

        private string choiceTypeLabel => choiceType == "Work" ? "VOEAdditionalOutposts.PrisonersWork".Translate().RawText : choiceType == "Recruit" ? "VOEAdditionalOutposts.RecruitPrisoners".Translate().RawText : choiceType == "Hemogen" ? "VOEAdditionalOutposts.HemogenPrisoners".Translate().RawText : "VOEAdditionalOutposts.EnslavePrisoners".Translate().RawText;

        MapParent RecruitMP;
        MapParent EnslaveMP;

        public override void Produce()
        {
            List<Thing> items = new List<Thing>();
            int FineSum = PaymentSilver(choiceType == "Work");
            items.AddRange(ThingDefOf.Silver.Make(FineSum));
            if (choiceType == "Hemogen")
                items.AddRange(ThingDefOf.HemogenPack.Make(HemogenAmount()));
            if (items.Count() > 0)
                Deliver(items);
            else
                Find.LetterStack.ReceiveLetter("VOEAdditionalOutposts.Letters.PrisonFail.Label".Translate(Name), "VOEAdditionalOutposts.Letters.PrisonFail.Text".Translate(), LetterDefOf.NeutralEvent);
        }

        public int PaymentSilver(bool IsPrisonersWorking = false)
        {
            int PrisonersWork = 0;
            if (IsPrisonersWorking)
            {
                PrisonersWork += PerPrisonerSkill * prisoners.Sum((Pawn p) => p.skills.skills.Max((SkillRecord sr) => p.skills.GetSkill(sr.def).Level));
            }
            return (int)((PerSocial * wardens.Skip(prisoners.Count()).Sum((Pawn p) => p.skills.GetSkill(SkillDefOf.Social).Level) + PrisonersWork) * OutpostsMod.Settings.ProductionMultiplier);
        }
        public int HemogenAmount()
        {
            return (TicksPerProduction / TicksPerHemogen) * prisoners.Count();
        }
        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 2000 == 0)
            {
                if (choiceType == "Recruit")
                {
                    int interactionInterval = InteractionInterval();
                    int wi = 0, pi = 0;
                    List<Pawn> wardensCurrent = wardens.ToList(), prisonersCurrent = prisoners.ToList();
                    while (wi < wardensCurrent.Count() && pi < prisonersCurrent.Count())
                    {
                        Pawn prisoner = prisonersCurrent[pi];
                        int interactPrisoner = prisoner.mindState.lastAssignedInteractTime;
                        Pawn warden = wardensCurrent[wi];
                        int interactWarden = warden.mindState.lastAssignedInteractTime;
                        if (interactWarden <= Find.TickManager.TicksGame)
                        {
                            if (prisoner.guest.Recruitable && interactPrisoner <= Find.TickManager.TicksGame)
                            {
                                if (interactWarden < 0)
                                    warden.mindState.lastAssignedInteractTime = Find.TickManager.TicksGame;
                                warden.mindState.lastAssignedInteractTime += interactionInterval;
                                if (interactPrisoner < 0)
                                    prisoner.mindState.lastAssignedInteractTime = Find.TickManager.TicksGame;
                                prisoner.mindState.lastAssignedInteractTime += interactionInterval;
                                string letterText = null;
                                string letterLabel = null;
                                LetterDef letterDef = null;
                                LookTargets lookTargets = null;
                                List<RulePackDef> extraSentencePacks = new List<RulePackDef>();
                                if (prisoner.guest.resistance > 0f)
                                {
                                    float statValue = warden.GetStatValue(StatDefOf.NegotiationAbility);
                                    float num1 = 1f;
                                    num1 *= statValue;
                                    float resistance = prisoner.guest.resistance;
                                    num1 = Mathf.Min(num1, resistance);
                                    prisoner.guest.resistance = Mathf.Max(0f, resistance - num1);
                                }
                                else
                                {
                                    InteractionWorker_RecruitAttempt.DoRecruit(warden, prisoner, out letterLabel, out letterText, useAudiovisualEffects: true, sendLetter: false);
                                    if (!letterLabel.NullOrEmpty())
                                    {
                                        letterDef = LetterDefOf.PositiveEvent;
                                    }
                                    lookTargets = new LookTargets(this);
                                    extraSentencePacks.Add(RulePackDefOf.Sentence_RecruitAttemptAccepted);
                                    PlayLogEntry_Interaction playLogEntry_Interaction = new PlayLogEntry_Interaction(InteractionDefOf.RecruitAttempt, warden, prisoner, extraSentencePacks);
                                    Find.PlayLog.Add(playLogEntry_Interaction);
                                    if (letterDef != null)
                                    {
                                        string text = playLogEntry_Interaction.ToGameStringFromPOV(warden);
                                        if (!letterText.NullOrEmpty())
                                        {
                                            text = text + "\n\n" + letterText;
                                        }
                                        Find.LetterStack.ReceiveLetter(letterLabel, text, letterDef, lookTargets);
                                    }
                                    if (RecruitMP != null)
                                    {
                                        if (RecruitMP is Outpost outpost)
                                        {
                                            outpost.AddPawn(RemovePawn(prisoner));
                                        }
                                        else
                                        {
                                            Map temp = deliveryMap;
                                            deliveryMap = RecruitMP.Map;
                                            Deliver(new List<Thing>() { RemovePawn(prisoner) });
                                            deliveryMap = temp;
                                        }
                                    }
                                }
                                wi++;
                            }
                            pi++;
                        }
                        else
                        {
                            wi++;
                        }
                    }
                }
                else if (choiceType == "Enslave")
                {
                    int interactionInterval = InteractionInterval();
                    int wi = 0, pi = 0;
                    List<Pawn> wardensCurrent = wardens.ToList(), prisonersCurrent = prisoners.ToList();
                    while (wi < wardensCurrent.Count() && pi < prisonersCurrent.Count())
                    {
                        Pawn prisoner = prisonersCurrent[pi];
                        int interactPrisoner = prisoner.mindState.lastAssignedInteractTime;
                        Pawn warden = wardensCurrent[wi];
                        int interactWarden = warden.mindState.lastAssignedInteractTime;
                        if (interactWarden <= Find.TickManager.TicksGame)
                        {
                            if (interactPrisoner <= Find.TickManager.TicksGame)
                            {
                                if (interactWarden < 0)
                                    warden.mindState.lastAssignedInteractTime = Find.TickManager.TicksGame;
                                warden.mindState.lastAssignedInteractTime += interactionInterval;
                                if (interactPrisoner < 0)
                                    prisoner.mindState.lastAssignedInteractTime = Find.TickManager.TicksGame;
                                prisoner.mindState.lastAssignedInteractTime += interactionInterval;
                                string letterText = null;
                                string letterLabel = null;
                                LetterDef letterDef = null;
                                LookTargets lookTargets = null;
                                List<RulePackDef> extraSentencePacks = new List<RulePackDef>();
                                if (prisoner.guest.will > 0f)
                                {
                                    float statValue = warden.GetStatValue(StatDefOf.NegotiationAbility);
                                    float num1 = 1f;
                                    num1 *= statValue;
                                    float will = prisoner.guest.will;
                                    num1 = Mathf.Min(num1, will);
                                    prisoner.guest.will = Mathf.Max(0f, will - num1);
                                }
                                else
                                {
                                    QuestUtility.SendQuestTargetSignals(prisoner.questTags, "Enslaved", prisoner.Named("SUBJECT"));
                                    GenGuest.EnslavePrisoner(warden, prisoner);
                                    letterLabel = "LetterLabelEnslavementSuccess".Translate() + ": " + prisoner.LabelCap;
                                    letterText = "LetterEnslavementSuccess".Translate(warden, prisoner);
                                    letterDef = LetterDefOf.PositiveEvent;
                                    lookTargets = new LookTargets(this);
                                    extraSentencePacks.Add(RulePackDefOf.Sentence_RecruitAttemptAccepted);
                                    PlayLogEntry_Interaction playLogEntry_Interaction = new PlayLogEntry_Interaction(InteractionDefOf.RecruitAttempt, warden, prisoner, extraSentencePacks);
                                    Find.PlayLog.Add(playLogEntry_Interaction);
                                    if (letterDef != null)
                                    {
                                        string text = playLogEntry_Interaction.ToGameStringFromPOV(warden);
                                        if (!letterText.NullOrEmpty())
                                        {
                                            text = text + "\n\n" + letterText;
                                        }
                                        Find.LetterStack.ReceiveLetter(letterLabel, text, letterDef, lookTargets);
                                    }
                                    if (EnslaveMP != null)
                                    {
                                        if (EnslaveMP is Outpost outpost)
                                        {
                                            outpost.AddPawn(RemovePawn(prisoner));
                                        }
                                        else
                                        {
                                            Map temp = deliveryMap;
                                            deliveryMap = EnslaveMP.Map;
                                            Deliver(new List<Thing>() { RemovePawn(prisoner) });
                                            deliveryMap = temp;
                                        }
                                    }
                                }
                                wi++;
                            }
                            pi++;
                        }
                        else
                        {
                            wi++;
                        }
                    }
                }
            }
        }

        public int InteractionInterval()
        {
            return Mathf.Max(MaxInteractionInterval - (int)Mathf.Max((Mathf.Max(wardens.Count() - prisoners.Count(), 0f) / Mathf.Max(prisoners.Count(), 1f)) * InteractionIntervalReductionPerWarden * (MaxInteractionInterval - Mathf.Min(MinInteractionInterval, MaxInteractionInterval))), Mathf.Min(MinInteractionInterval, MaxInteractionInterval));
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
                    List<FloatMenuOption> FMO = new List<FloatMenuOption>();
                    FMO.Add(new FloatMenuOption("VOEAdditionalOutposts.PrisonersWork".Translate().RawText + ": " + "VOEAdditionalOutposts.Silver".Translate(PaymentSilver(true).ToString()).RawText, delegate
                    {
                        choiceType = "Work";
                    }, ThingDefOf.Silver));
                    FMO.Add(new FloatMenuOption("VOEAdditionalOutposts.RecruitPrisoners".Translate().RawText + " + " + "VOEAdditionalOutposts.Silver".Translate(PaymentSilver().ToString()).RawText, delegate
                    {
                        choiceType = "Recruit";
                    }, ContentFinder<Texture2D>.Get("Icons/BorderPostImprison"), Color.white));
                    if (ModsConfig.IdeologyActive)
                    {
                        FMO.Add(new FloatMenuOption("VOEAdditionalOutposts.EnslavePrisoners".Translate().RawText + " + " + "VOEAdditionalOutposts.Silver".Translate(PaymentSilver().ToString()).RawText, delegate
                        {
                            choiceType = "Enslave";
                        }, ContentFinder<Texture2D>.Get("Icons/BorderPostImprison"), ColorLibrary.Orange));
                    }
                    if (ModsConfig.BiotechActive)
                    {
                        FMO.Add(new FloatMenuOption("VOEAdditionalOutposts.HemogenPrisoners".Translate().RawText + ": " + "VOEAdditionalOutposts.Hemogen".Translate(HemogenAmount().ToString()).RawText + " + " + "VOEAdditionalOutposts.Silver".Translate(PaymentSilver().ToString()).RawText, delegate
                        {
                            choiceType = "Hemogen";
                        }, ThingDefOf.HemogenPack));
                    }
                    Find.WindowStack.Add(new FloatMenu(FMO));
                },
                defaultLabel = choiceTypeLabel,
                defaultDesc = ChooseExt.ChooseDesc,
                icon = choiceType == "Work" ? ThingDefOf.Silver.uiIcon : choiceType == "Hemogen" ? ThingDefOf.HemogenPack.uiIcon : TexOutposts.ImprisonTex,
                defaultIconColor = choiceType == "Enslave" ? ColorLibrary.Orange : Color.white
            };
            yield return new Command_Action
            {
                action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(new FloatMenuOption(this.LabelCap, delegate
                    {
                        RecruitMP = null;
                    },
                    iconTex: this.ExpandingIcon,
                    iconColor: this.ExpandingIconColor));
                    foreach (Map map in from m in Find.Maps
                                        where m.IsPlayerHome
                                        orderby Find.WorldGrid.ApproxDistanceInTiles(m.Parent.Tile, this.Tile)
                                        select m)
                    {
                        list.Add(new FloatMenuOption(map.Parent.LabelCap, delegate
                        {
                            RecruitMP = map.Parent;
                        },
                        iconTex: map.Parent.ExpandingIcon,
                        iconColor: map.Parent.ExpandingIconColor));
                    }
                    foreach (MapParent mapParent in from m in Find.WorldObjects.AllWorldObjects.OfType<Outpost>()
                                                    where m.Tile != this.Tile
                                                    orderby Find.WorldGrid.ApproxDistanceInTiles(m.Tile, this.Tile)
                                                    select m)
                    {
                        list.Add(new FloatMenuOption(mapParent.LabelCap, delegate
                        {
                            RecruitMP = mapParent;
                        },
                        iconTex: mapParent.ExpandingIcon,
                        iconColor: mapParent.ExpandingIconColor));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                },
                defaultLabel = "VOEAdditionalOutposts.Commands.RecruitDelivery.Label".Translate(),
                defaultDesc = "VOEAdditionalOutposts.Commands.RecruitDelivery.Desc".Translate(RecruitMP != null ? RecruitMP.LabelCap : this.LabelCap),
                icon = RecruitMP != null ? RecruitMP.ExpandingIcon : this.ExpandingIcon,
                defaultIconColor = Color.white
            };
            if (ModsConfig.IdeologyActive)
            {
                yield return new Command_Action
                {
                    action = delegate
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        list.Add(new FloatMenuOption(this.LabelCap, delegate
                        {
                            EnslaveMP = null;
                        },
                        iconTex: this.ExpandingIcon,
                        iconColor: this.ExpandingIconColor));
                        foreach (Map map in from m in Find.Maps
                                            where m.IsPlayerHome
                                            orderby Find.WorldGrid.ApproxDistanceInTiles(m.Parent.Tile, this.Tile)
                                            select m)
                        {
                            list.Add(new FloatMenuOption(map.Parent.LabelCap, delegate
                            {
                                EnslaveMP = map.Parent;
                            },
                            iconTex: map.Parent.ExpandingIcon,
                            iconColor: map.Parent.ExpandingIconColor));
                        }
                        foreach (MapParent mapParent in from m in Find.WorldObjects.AllWorldObjects.OfType<Outpost>()
                                                        where m.Tile != this.Tile
                                                        orderby Find.WorldGrid.ApproxDistanceInTiles(m.Tile, this.Tile)
                                                        select m)
                        {
                            list.Add(new FloatMenuOption(mapParent.LabelCap, delegate
                            {
                                EnslaveMP = mapParent;
                            },
                            iconTex: mapParent.ExpandingIcon,
                            iconColor: mapParent.ExpandingIconColor));
                        }
                        Find.WindowStack.Add(new FloatMenu(list));
                    },
                    defaultLabel = "VOEAdditionalOutposts.Commands.EnslaveDelivery.Label".Translate(),
                    defaultDesc = "VOEAdditionalOutposts.Commands.EnslaveDelivery.Desc".Translate(EnslaveMP != null ? EnslaveMP.LabelCap : this.LabelCap),
                    icon = EnslaveMP != null ? EnslaveMP.ExpandingIcon : this.ExpandingIcon,
                    defaultIconColor = ColorLibrary.Orange
                };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref choiceType, "choiceType");
            Scribe_References.Look(ref RecruitMP, "RecruitMP");
            Scribe_References.Look(ref EnslaveMP, "EnslaveMP");
        }

        public override string ProductionString()
        {
            if (Ext == null || choiceType == null)
            {
                return "";
            }
            return "VOEAdditionalOutposts.WillWarden".Translate(wardens.Count(), prisoners.Count(), choiceTypeLabel, TimeTillProduction, "VOEAdditionalOutposts.Silver".Translate(PaymentSilver(choiceType == "Work").ToString()).RawText + ((choiceType == "Hemogen") ? "\n" + "VOEAdditionalOutposts.Hemogen".Translate(HemogenAmount().ToString()).RawText : "")).RawText;
        }
    }
}
