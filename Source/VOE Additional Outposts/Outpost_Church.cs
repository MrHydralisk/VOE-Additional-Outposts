using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Outposts;
using Verse;
using UnityEngine;

namespace VOEAdditionalOutposts
{
    public class Outpost_Church : Outpost
    {
        [PostToSetings("VOEAdditionalOutposts.Settings.PerSocial", PostToSetingsAttribute.DrawMode.IntSlider, 25, 10, 100, null, null)]
        public int PerSocial = 25;

        [PostToSetings("VOEAdditionalOutposts.Settings.InteractionIntervalReductionPerPriest", PostToSetingsAttribute.DrawMode.Percentage, 0.2f, 0, 0.5f, null, null)]
        public float InteractionIntervalReductionPerWarden = 0.2f;

        [PostToSetings("VOEAdditionalOutposts.Settings.MaxInteractionInterval", PostToSetingsAttribute.DrawMode.IntSlider, 30000, 15000, 60000, null, null)]
        public int MaxInteractionInterval = 30000;

        [PostToSetings("VOEAdditionalOutposts.Settings.MinInteractionInterval", PostToSetingsAttribute.DrawMode.IntSlider, 10000, 5000, 60000, null, null)]
        public int MinInteractionInterval = 10000;

        private List<Ideo> ideologies => base.AllPawns.Where((Pawn p) => !p.IsPrisoner).Select((Pawn p) => p.Ideo).Distinct().ToList();

        private Ideo ChooseIdeologyCached;

        protected Ideo ChooseIdeology => ChooseIdeologyCached ?? ideologies.First();

        private List<Pawn> priests => base.AllPawns.Where((Pawn p) =>! p.IsPrisoner && p.Ideo == ChooseIdeology && !StatDefOf.ConversionPower.Worker.IsDisabledFor(p)).OrderByDescending((Pawn p) => p.GetStatValue(StatDefOf.ConversionPower)).ToList();

        private List<Pawn> followers => base.AllPawns.Where((Pawn p) => p.Ideo != ChooseIdeology).OrderBy((Pawn p) => p.ideo.Certainty).Select((Pawn p) => (p, p.IsPrisoner ? 0 : p.IsSlave ? 1 : 2)).OrderByDescending(x => x.Item2).Select(x => x.p).ToList();
        
        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;
        
        public override void Produce()
        {
            List<Thing> items = new List<Thing>();
            int FineSum = PaymentSilver();
            items.AddRange(ThingDefOf.Silver.Make(FineSum));
            if (items.Count() > 0)
                Deliver(items);
            else
                Find.LetterStack.ReceiveLetter("VOEAdditionalOutposts.Letters.ChurchFail.Label".Translate(Name), "VOEAdditionalOutposts.Letters.ChurchFail.Text".Translate(), LetterDefOf.NeutralEvent);
        }

        public int PaymentSilver(Ideo ideo)
        {
            return (int)((PerSocial * base.AllPawns.Where((Pawn p) => !p.IsPrisoner && p.Ideo == ideo && !StatDefOf.ConversionPower.Worker.IsDisabledFor(p)).OrderByDescending((Pawn p) => p.GetStatValue(StatDefOf.ConversionPower)).Skip(base.AllPawns.Where((Pawn p) => p.Ideo != ideo).Count()).Sum((Pawn p) => p.skills.GetSkill(SkillDefOf.Social).Level)) * OutpostsMod.Settings.ProductionMultiplier);
        }

        public int PaymentSilver()
        {
            return (int)((PerSocial * priests.Skip(followers.Count()).Sum((Pawn p) => p.skills.GetSkill(SkillDefOf.Social).Level)) * OutpostsMod.Settings.ProductionMultiplier);
        }

        public override void Tick()
        {
            base.Tick();
            int interactionInterval = InteractionInterval();
            int pi = 0, fi = 0;
            List<Pawn> priestsCurrent = priests.ToList(), followersCurrent = followers.ToList();
            while (pi < priestsCurrent.Count() && fi < followersCurrent.Count())
            {
                Pawn follower = followersCurrent[fi];
                Pawn priest = priestsCurrent[pi];
                if (follower.mindState.lastAssignedInteractTime <= Find.TickManager.TicksGame)
                {
                    if (follower.mindState.lastAssignedInteractTime < 0)
                        follower.mindState.lastAssignedInteractTime = Find.TickManager.TicksGame;
                    follower.mindState.lastAssignedInteractTime += interactionInterval;
                    string letterText = null;
                    string letterLabel = null;
                    LetterDef letterDef = null;
                    LookTargets lookTargets = null;
                    List<RulePackDef> extraSentencePacks = new List<RulePackDef>();
                    Ideo ideo = follower.Ideo;
                    Precept_Role role = ideo.GetRole(follower);
                    float certainty = follower.ideo.Certainty;
                    if (follower.ideo.IdeoConversionAttempt(InteractionWorker_ConvertIdeoAttempt.CertaintyReduction(priest, follower), priest.Ideo))
                    {
                        if (PawnUtility.ShouldSendNotificationAbout(priest) || PawnUtility.ShouldSendNotificationAbout(follower))
                        {
                            letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                            letterText = "LetterConvertIdeoAttempt_Success".Translate(priest.Named("INITIATOR"), follower.Named("RECIPIENT"), priest.Ideo.Named("IDEO"), ideo.Named("OLDIDEO")).Resolve();
                            letterDef = LetterDefOf.PositiveEvent;
                            lookTargets = new LookTargets(priest, follower);
                            if (role != null)
                            {
                                letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(follower.Named("PAWN"), role.Named("ROLE"), ideo.Named("OLDIDEO")).Resolve();
                            }
                        }
                        extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptSuccess);
                    }
                    if (!letterLabel.NullOrEmpty())
                    {
                        letterDef = LetterDefOf.PositiveEvent;
                    }
                    PlayLogEntry_Interaction playLogEntry_Interaction = new PlayLogEntry_Interaction(InteractionDefOf.ConvertIdeoAttempt, priest, follower, extraSentencePacks);
                    Find.PlayLog.Add(playLogEntry_Interaction);
                    if (letterDef != null)
                    {
                        string text = playLogEntry_Interaction.ToGameStringFromPOV(priest);
                        if (!letterText.NullOrEmpty())
                        {
                            text = text + "\n\n" + letterText;
                        }
                        Find.LetterStack.ReceiveLetter(letterLabel, text, letterDef, lookTargets);
                    }
                    pi++;
                }
                fi++;
            }
        }

        public int InteractionInterval()
        {
            return Mathf.Max(MaxInteractionInterval - (int)Mathf.Max((Mathf.Max(priests.Count() - followers.Count(), 0f) / Mathf.Max(followers.Count(), 1f)) * InteractionIntervalReductionPerWarden * (MaxInteractionInterval - Mathf.Min(MinInteractionInterval, MaxInteractionInterval))), Mathf.Min(MinInteractionInterval, MaxInteractionInterval));
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Append(new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new FloatMenu(ideologies.Select(delegate (Ideo ideo)
                    {
                        return new FloatMenuOption("VOEAdditionalOutposts.SpreadIdeology".Translate(base.AllPawns.Where((Pawn p) => !p.IsPrisoner && p.Ideo == ideo).Count(), ideo.name).RawText + " + " + "VOEAdditionalOutposts.Silver".Translate(PaymentSilver(ideo)).RawText, delegate
                        {
                            ChooseIdeologyCached = ideo;
                        }, itemIcon: ideo.iconDef.Icon, iconColor: ideo.colorDef.color);
                    })
                        .ToList()));
                },
                defaultLabel = ChooseExt.ChooseLabel.Formatted(ChooseIdeology.name.ToStringSafe()),
                defaultDesc = ChooseExt.ChooseDesc,
                icon = ChooseIdeology.iconDef.Icon,
                defaultIconColor = ChooseIdeology.colorDef.color
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ChooseIdeologyCached, "ChooseIdeologyCached");
        }
        
        public override string ProductionString()
        {
            if (Ext == null || ChooseIdeology == null)
            {
                return "";
            }
            return "VOEAdditionalOutposts.WillSpreadIdeology".Translate(priests.Count(), followers.Count(), ChooseIdeology.name, TimeTillProduction, "VOEAdditionalOutposts.Silver".Translate(PaymentSilver().ToString()).RawText).RawText;
        }
    }
}
