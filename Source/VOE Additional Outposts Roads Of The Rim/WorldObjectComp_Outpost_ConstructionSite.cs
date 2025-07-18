using Outposts;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts_RoadsOfTheRim
{
    public class WorldObjectComp_Outpost_ConstructionSite : WorldObjectComp
    {
        public bool currentlyWorkingOnSite;

        public RoadConstructionSite site
        {
            get
            {
                if (siteCashed == null || !siteCashed.Spawned || siteCashed.Destroyed || siteCashed.GetNextLeg() == null)
                {
                    List<RoadConstructionSite> roadConstructionSites = Find.WorldObjects.AllWorldObjects.OfType<RoadConstructionSite>().Where(rcs => rcs.Spawned).ToList();
                    if (roadConstructionSites.NullOrEmpty())
                    {
                        currentlyWorkingOnSite = false;
                        siteCashed = null;
                        compCashed = null;
                    }
                    else
                    {
                        siteCashed = roadConstructionSites.MinBy(s => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, parent.Tile));
                        compCashed = siteCashed.GetComponent<WorldObjectComp_ConstructionSite>();
                    }
                }
                return siteCashed;
            }
        }
        public WorldObjectComp_ConstructionSite comp
        {
            get
            {
                if (compCashed == null)
                {
                    compCashed = siteCashed.GetComponent<WorldObjectComp_ConstructionSite>();
                }
                return compCashed;
            }
        }
        private RoadConstructionSite siteCashed;
        private WorldObjectComp_ConstructionSite compCashed;

        public Outpost outpost => parent as Outpost;

        public override void CompTick()
        {
            if (currentlyWorkingOnSite && Find.TickManager.TicksGame % 100 == 0 && site != null)
            {
                DoSomeWork();
            }
        }

        public void DoSomeWork()
        {
            int num = 2;
            float num2 = 1f;
            float num3 = AmountOfWork();
            float num4 = (comp.GetLeft("Work") - num3) / (float)comp.GetCost("Work");
            //TeachPawns(num2);
            if (num > 0 && site.roadDef.defName != "DirtPathBuilt")
            {
                num3 = num3 * 0.25f * (float)num;
            }
            num3 = num2 * num3;
            comp.UpdateProgress(num3);
        }

        public float AmountOfWork()
        {
            List<Pawn> pawnsListForReading = outpost.CapablePawns.ToList();
            DefModExtension_RotR_RoadDef defModExtension_RotR_RoadDef = null;
            try
            {
                defModExtension_RotR_RoadDef = site.roadDef.GetModExtension<DefModExtension_RotR_RoadDef>();
            }
            catch
            {
            }
            float num = 0f;
            float num2 = 0f;
            float num3 = 0f;
            foreach (Pawn item in pawnsListForReading)
            {
                float num4 = PawnBuildingUtility.ConstructionValue(item);
                if (PawnBuildingUtility.HealthyColonist(item))
                {
                    num += num4;
                    if (defModExtension_RotR_RoadDef != null && (float)PawnBuildingUtility.ConstructionLevel(item) >= defModExtension_RotR_RoadDef.minConstruction)
                    {
                        num2 += num4;
                    }
                }
                else if (PawnBuildingUtility.HealthyPackAnimal(item))
                {
                    num3 += num4;
                }
            }
            if (defModExtension_RotR_RoadDef != null)
            {
                float num5 = num2 / num;
                if (num5 < defModExtension_RotR_RoadDef.percentageOfminConstruction)
                {
                    float num6 = num5 / defModExtension_RotR_RoadDef.percentageOfminConstruction;
                    num *= num6;
                }
            }
            if (num3 > num)
            {
                num3 = num;
            }
            return num + num3;
        }

        public void TeachPawns(float ratio)
        {
            ratio = Math.Max(Math.Min(1f, ratio), 0f);
            foreach (Pawn item in outpost.CapablePawns.ToList())
            {
                if (item.IsFreeColonist && item.health.State == PawnHealthState.Mobile && !item.RaceProps.packAnimal)
                {
                    item.skills.Learn(SkillDefOf.Construction, 3f * ratio);
                }
            }
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
                    Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, canTargetTiles: true);
                },
                defaultLabel = "RoadsOfTheRimAddConstructionSite".Translate(),
                defaultDesc = "RoadsOfTheRimAddConstructionSiteDescription".Translate(),
                icon = (Texture)(object)ContentFinder<Texture2D>.Get("UI/Commands/AddConstructionSite")
            };
            if (currentlyWorkingOnSite)
            {
                yield return new Command_Action
                {
                    action = delegate
                    {
                        currentlyWorkingOnSite = false;
                        siteCashed = null;
                    },
                    defaultLabel = "RoadsOfTheRimStopWorkingOnSite".Translate(),
                    defaultDesc = "RoadsOfTheRimStopWorkingOnSiteDescription".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/RemoveConstructionSite")
                };
            }
            else
            {
                yield return new Command_Action
                {
                    action = delegate
                    {
                        currentlyWorkingOnSite = true;
                    },
                    defaultLabel = "RoadsOfTheRimWorkOnSite".Translate(),
                    defaultDesc = "RoadsOfTheRimWorkOnSiteDescription".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/AddConstructionSite")
                };
            }
        }
        public static bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid || Find.World.Impassable(target.Tile))
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            RoadConstructionSite roadConstructionSite = (RoadConstructionSite)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite"));
            roadConstructionSite.Tile = target.Tile;
            Find.WorldObjects.Add(roadConstructionSite);
            ConstructionMenu constructionMenu = new ConstructionMenu(roadConstructionSite, null);
            if (constructionMenu.CountBuildableRoads() == 0)
            {
                Find.WorldObjects.Remove(roadConstructionSite);
                Messages.Message("RoadsOfTheRim_NoBetterRoadCouldBeBuilt".Translate(), MessageTypeDefOf.RejectInput);
            }
            else
            {
                constructionMenu.closeOnClickedOutside = true;
                constructionMenu.forcePause = true;
                Find.WindowStack.Add(constructionMenu);
            }
            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentlyWorkingOnSite, "currentlyWorkingOnSite");
        }
    }
}