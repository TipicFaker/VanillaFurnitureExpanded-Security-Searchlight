﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFESecurity
{
    public class ArtilleryComp : WorldObjectComp
    {
        public int recentRetaliationTicks;

        private const int BombardmentStartDelay = GenTicks.TicksPerRealSecond * 5;

        private WorldObjectCompProperties_Artillery Props => (WorldObjectCompProperties_Artillery)props;

        private ThingDef ArtilleryDef
        {
            get
            {
                if (cachedArtilleryDef == null)
                    cachedArtilleryDef = Props.ArtilleryDefFor(parent.Faction.def);
                return cachedArtilleryDef;
            }
        }

        public IEnumerable<CompLongRangeArtillery> ArtilleryComps => artillery.Select(a => a.TryGetComp<CompLongRangeArtillery>());

        private ThingDef ArtilleryGunDef => ArtilleryDef.building.turretGunDef;

        public bool Attacking
        {
            get
            {
                if (parent.Faction == Faction.OfPlayer)
                    return ArtilleryComps.Any(a => a.targetedTile != GlobalTargetInfo.Invalid);
                return bombardmentDurationTicks > 0;
            }
        }

        private bool CanAttack => ArtilleryDef != null && !Attacking && recentRetaliationTicks <= 0;

        private CompProperties_LongRangeArtillery ArtilleryProps => cachedArtilleryDef.GetCompProperties<CompProperties_LongRangeArtillery>();

        public IEnumerable<GlobalTargetInfo> Targets
        {
            get
            {
                return ArtilleryComps.Where(a => a.targetedTile != GlobalTargetInfo.Invalid).Select(a => a.targetedTile).Distinct();
            }
        }

        public IEnumerable<Map> PlayerMaps
        {
            get
            {
                var maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    var map = maps[i];
                    if (map.ParentFaction == Faction.OfPlayer)
                        yield return map;
                }
            }
        }

        private void TryResolveArtilleryCount()
        {
            if (artilleryCount <= 0)
            {
                artilleryCount = Props.ArtilleryCountFor(parent.Faction.def);
            }
        }

        public void BombardmentTick()
        {
            if (parent.Faction != Faction.OfPlayer)
            {
                TryResolveArtilleryCount();
                if (Attacking)
                {
                    // Try and bombard player settlements
                    if (artilleryCooldownTicks > 0)
                    {
                        artilleryCooldownTicks--;
                    }
                    else if (PlayerMaps.TryRandomElementByWeight(m => StorytellerUtility.DefaultThreatPointsNow(m), out Map targetMap))
                    {
                        if (artilleryWarmupTicks < 0)
                        {
                            artilleryWarmupTicks = ArtilleryDef.building.turretBurstWarmupTime.min.SecondsToTicks();
                        }
                        else
                        {
                            artilleryWarmupTicks--;
                            if (artilleryWarmupTicks <= 0)
                            {
                                var shell = ArtilleryStrikeUtility.GetRandomShellFor(ArtilleryGunDef, parent.Faction.def);
                                if (shell != null)
                                {
                                    float missRadius = ArtilleryStrikeUtility.FinalisedMissRadius(ArtilleryGunDef.Verbs[0].ForcedMissRadius, ArtilleryProps.maxForcedMissRadiusFactor, parent.Tile, targetMap.Tile, ArtilleryProps.worldTileRange);
                                    var strikeCells = ArtilleryStrikeUtility.PotentialStrikeCells(targetMap, missRadius);
                                    for (int i = 0; i < artilleryCount; i++)
                                    {
                                        ArtilleryStrikeUtility.SpawnArtilleryStrikeSkyfaller(shell, targetMap, strikeCells.RandomElement());
                                    }
                                    artilleryCooldownTicks = ArtilleryDef.building.turretBurstCooldownTime.SecondsToTicks();
                                }
                                else
                                {
                                    Log.ErrorOnce($"Tried to get shell for bombardment but got null instead (artilleryGunDef={ArtilleryGunDef}, factionDef={parent.Faction.def})", 8173352);
                                }
                            }
                        }
                    }
                    else
                    {
                        artilleryWarmupTicks = -1;
                    }

                    // Reduce the duration of the bombardment
                    bombardmentDurationTicks--;
                    if (bombardmentDurationTicks == 0)
                        EndBombardment();
                }
            }
        }

        public void TryStartBombardment()
        {
            if (CanAttack)
            {
                Find.World.GetComponent<WorldArtilleryTracker>().RegisterBombardment(parent);
                artilleryCooldownTicks = BombardmentStartDelay;
                bombardmentDurationTicks = Props.bombardmentDurationRange.RandomInRange;
                recentRetaliationTicks = Props.bombardmentCooldownRange.RandomInRange;
                Find.LetterStack.ReceiveLetter("VFESecurity.ArtilleryStrikeSettlement_Letter".Translate(), "VFESecurity.ArtilleryStrikeSettlement_LetterText".Translate(parent.Faction.def.pawnsPlural, parent.Label), LetterDefOf.ThreatBig, parent);
            }
        }

        private void EndBombardment()
        {
            Find.World.GetComponent<WorldArtilleryTracker>().bombardingWorldObjects.Remove(parent);
            Messages.Message("VFESecurity.Message_ArtilleryBombardmentEnded".Translate(parent.Faction.def.pawnsPlural.CapitalizeFirst(), parent.Label), MessageTypeDefOf.PositiveEvent);
        }

        public override void PostPostRemove()
        {
            if (Attacking)
                EndBombardment();
            Find.World.GetComponent<WorldArtilleryTracker>().DeregisterBombardment(parent);
            base.PostPostRemove();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref artilleryCount, "artilleryCount", -1);
            Scribe_Values.Look(ref artilleryWarmupTicks, "artilleryWarmupTicks", -1);
            Scribe_Values.Look(ref artilleryCooldownTicks, "artilleryCooldownTicks");
            Scribe_Values.Look(ref bombardmentDurationTicks, "bombardmentDurationTicks");
            Scribe_Values.Look(ref recentRetaliationTicks, "recentRetaliationTicks");
            Scribe_Collections.Look(ref artillery, "artillery", LookMode.Reference);
            base.PostExposeData();
        }

        private int artilleryCount = -1;
        private int artilleryWarmupTicks = -1;
        private int artilleryCooldownTicks;
        private int bombardmentDurationTicks;
        public HashSet<Thing> artillery = new HashSet<Thing>();

        private ThingDef cachedArtilleryDef;
    }
}