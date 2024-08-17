using System;
using System.Collections.Generic;
using System.Linq;
using OS_WarPods;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

[StaticConstructorOnStartup]
public class CompUtilityLaunchable : ThingComp
{

    public static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment");

    public static readonly Texture2D LaunchCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

    public static readonly Texture2D TargetCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport");

    public static readonly Texture2D CancelCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

    public static readonly Texture2D InspectCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Study");


    //CompLaunchable justheretolookat;
    //CompProperties_Launchable thistoo;
    //CompTransporter andthisaswell;
    //TransportPodsArrivalActionUtility

    private const float FuelPerTile = 2.25f;

    public CompProperties_UtilityLaunchable Props => (CompProperties_UtilityLaunchable)props;

    //public Building FuelingPortSource => FuelingPortUtility.FuelingPortGiverAtFuelingPortCell(parent.Position, parent.Map);

    private IEnumerable<ThingWithComps> launchGroup = Enumerable.Empty<ThingWithComps>();
    public IEnumerable<Building> launchGroupFuelingPorts => from x in launchGroup select FuelingPortUtility.FuelingPortGiverAtFuelingPortCell(x.Position, x.Map);

    public bool ReadyForLaunch
    {
        get
        {
            if (ConnectedToFuelingPort && FuelingPortSourceHasAnyFuel && !IsUnderRoof)
            {
                return true;
            }
            return false;
        }
    }

    public bool ConnectedToFuelingPort
    {
        get
        {
            if (Props.requireFuel)
            {
                return launchGroupFuelingPorts.Count(x => x != null) == launchGroup.Count();
            }
            return true;
        }
    }

    public bool FuelingPortSourceHasAnyFuel
    {
        get
        {
            if (Props.requireFuel)
            {
                if (ConnectedToFuelingPort)
                {
                    return launchGroupFuelingPorts.Count(x => x.GetComp<CompRefuelable>().HasFuel) == launchGroup.Count();
                }
                return false;
            }
            return true;
        }
    }
    
    public bool IsUnderRoof
    {
        get
        {
            if (launchGroup.Count(x => x.Position.Roofed(x.Map)) < launchGroup.Count())//this.parent.Position.Roofed(parent.Map))
            {
                return true;
            }
            return false;
        }
    }

    public int MaxLaunchDistance
    {
        get
        {
            if (!ReadyForLaunch)
            {
                return 0;
            }
            if (Props.fixedLaunchDistanceMax >= 0)
            {
                return Props.fixedLaunchDistanceMax;
            }
            return MaxLaunchDistanceAtFuelLevel(launchGroupFuelingPorts.Min(x => x.GetComp<CompRefuelable>().Fuel));
        }
    }

    //public int target_tile;
    //public IntVec3 target_cell;

    public GlobalTargetInfo global_target = GlobalTargetInfo.Invalid;
    public LocalTargetInfo local_target = LocalTargetInfo.Invalid;

    public CompUtilityLaunchable()
    {

    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref global_target, "global_target", GlobalTargetInfo.Invalid);
        Scribe_Values.Look(ref local_target, "local_target", LocalTargetInfo.Invalid);

        //Scribe_Values.Look(ref target_tile, "target_tile", -1);
        //Scribe_Values.Look(ref target_cell, "target_cell", new IntVec3(0,0,0));
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo item in base.CompGetGizmosExtra())
        {
            yield return item;
        }
        if (!ReadyForLaunch)
        {
            yield break;
        }

        // FILL LAUNCH GROUP AT THIS STAGE
        launchGroup = from x in Find.Selector.SelectedObjectsListForReading where (((Thing)x).TryGetComp<CompUtilityLaunchable>() != null && ((Thing)x).def == parent.def) select (ThingWithComps)x;


        // --- VALIDATION CHECK: make sure targeting is not corrupted by map changes
        // 1st check: will the tile always exist?
        // > yes, don't worry.
        // 2nd check: will the cell always exist?
        if (local_target.IsValid)
        {
            if(Find.Maps.Where(map => map.Tile == global_target.Tile).FirstOrDefault() == null)
            {
                local_target = LocalTargetInfo.Invalid;
                Messages.Message("Map at " + global_target.Tile.ToString() + " is out of view, causing loss of " + parent.Label + "targeting calibration.", MessageTypeDefOf.NeutralEvent);
            }
        }
        // --- end of validation section


        // COMMAND - launch pod - alt: set target and launch pod

        Command_Action cmd = new Command_Action
        {
            action = delegate
            {
                TryLaunch();
            },
            defaultLabel = "CommandLaunchGroup".Translate(),
            defaultDesc = "CommandLaunchGroupDesc".Translate(),
            icon = LaunchCommandTex,
            alsoClickIfOtherInGroupClicked = false // true by default

        };
        if (!ConnectedToFuelingPort)
        {
            cmd.Disable("CommandLaunchGroupFailNotConnectedToFuelingPort".Translate());
        }
        else if (!FuelingPortSourceHasAnyFuel)
        {
            cmd.Disable("CommandLaunchGroupFailNoFuel".Translate());
        }
        else if (IsUnderRoof)
        {
            cmd.Disable("CommandLaunchGroupFailUnderRoof".Translate());
        }
        if (!global_target.IsValid)//if (target_tile < 0)
        {
            cmd.action = delegate
            {
                StartChoosingDestination();
                TryLaunch();
            };
        }
        else
        {
            cmd.action = delegate
            {
                TryLaunch();
            };
        }


        yield return cmd;

        // COMMAND - Set target tile

        cmd = new Command_Action
        {
            action = delegate
            {
                StartChoosingDestination();
            },
            defaultLabel = "Set target",
            defaultDesc = "Set target coordinates before launch.",
            icon = TargetCommandTex,
            alsoClickIfOtherInGroupClicked = false
        };

        if (!ConnectedToFuelingPort)
        {
            cmd.Disable("CommandLaunchGroupFailNotConnectedToFuelingPort".Translate());
        }
        else if (!FuelingPortSourceHasAnyFuel)
        {
            cmd.Disable("CommandLaunchGroupFailNoFuel".Translate());
        }
        else if (IsUnderRoof)
        {
            cmd.Disable("CommandLaunchGroupFailUnderRoof".Translate());
        }
        else if (global_target != GlobalTargetInfo.Invalid)
        {
            cmd.Disable("Target already set.");
        }

        yield return cmd;

        // COMMAND - Cancel set targeting
        if (global_target != GlobalTargetInfo.Invalid)
        {
            yield return new Command_Action
            {
                action = delegate
                {
                    global_target = GlobalTargetInfo.Invalid;
                    local_target = LocalTargetInfo.Invalid;
                },
                defaultLabel = "Cancel targeting",
                defaultDesc = "Cancel targeting instructions.",
                icon = CancelCommandTex
                //alsoClickIfOtherInGroupClicked = true // true by default
            };
        }

        // COMMAND - Inspect set targeting
        if (global_target.IsValid)
        {
            yield return new Command_Action
            {
                action = delegate
                {
                    //WorldObject target = Find.WorldObjects.AllWorldObjects.Where(x => x.Tile == global_target.Tile).FirstOrDefault();
                    LookTargets looktargets;
                    if (local_target.IsValid)
                    {
                        looktargets = new LookTargets(launchGroup.Select(x => x.TryGetComp<CompUtilityLaunchable>().local_target.ToGlobalTargetInfo(global_target.Map)));
                    }
                    else if (global_target.HasWorldObject)
                    {
                        looktargets = new LookTargets(launchGroup.Select(x => x.TryGetComp<CompUtilityLaunchable>().global_target.WorldObject));
                    }
                    else
                    {
                        looktargets = new LookTargets(launchGroup.Select(x => x.TryGetComp<CompUtilityLaunchable>().global_target));
                    }
                    CameraJumper.TryJumpAndSelect(looktargets.TryGetPrimaryTarget());
                },
                defaultLabel = "Inspect target",
                defaultDesc = "View the targeted location.",
                icon = InspectCommandTex,
                alsoClickIfOtherInGroupClicked = false
            };
        }
            
    }

    public override string CompInspectStringExtra()
    {
        if (ReadyForLaunch)
        {
            if (!ConnectedToFuelingPort)
            {
                return "NotReadyForLaunch".Translate() + ": " + "NotAllInGroupConnectedToFuelingPort".Translate().CapitalizeFirst() + ".";
            }
            if (!FuelingPortSourceHasAnyFuel)
            {
                return "NotReadyForLaunch".Translate() + ": " + "NotAllFuelingPortSourcesInGroupHaveAnyFuel".Translate().CapitalizeFirst() + ".";
            }
            /*if (AnyInGroupHasAnythingLeftToLoad)
            {
                return "NotReadyForLaunch".Translate() + ": " + "TransportPodInGroupHasSomethingLeftToLoad".Translate().CapitalizeFirst() + ".";
            }*/
            return "ReadyForLaunch".Translate();
        }
        return null;
    }

    public void TryLaunch()
    {
        if (!parent.Spawned)
        {
            Log.Error(string.Concat("Tried to launch ", parent, ", but it's unspawned."));
            return;
        }
        if (!global_target.IsValid)
        {
            Log.Error(string.Concat("Tried to launch ", parent, ", but pod lacks valid targeting data."));
            return;
        }
        else
        {
            if (!ReadyForLaunch || !ConnectedToFuelingPort || !FuelingPortSourceHasAnyFuel)
            {
                return;
            }
            Map map = parent.Map;
            int num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, global_target.Tile);
            if (num <= MaxLaunchDistance)
            {
                float amount = Mathf.Max(FuelNeededToLaunchAtDist(num), 1f);
                launchGroupFuelingPorts.AsParallel().ForAll(x => x.TryGetComp<CompRefuelable>().ConsumeFuel(amount));
                foreach (ThingWithComps launchable in launchGroup)
                {
                    UtilityPodDef governor = launchable.TryGetComp<CompUtilityLaunchable>().Props.utilityPodDef;//DefDatabase<UtilityPodDef>.GetNamed("JustOneExampleDefName")
                    UtilityPodLeaving cl = (UtilityPodLeaving)SkyfallerMaker.MakeSkyfaller(governor.leavingDef);
                    cl.utilityPodDef = governor;
                    cl.destinationTile = global_target.Tile;
                    cl.cell = local_target.Cell;
                    launchable.Destroy();
                    GenSpawn.Spawn(cl, launchable.Position, map);
                }
                CameraJumper.TryHideWorld();
            }
        }

    }

    /*public void TryLaunch()
    {
        if (!parent.Spawned)
        {
            Log.Error(string.Concat("Tried to launch ", parent, ", but it's unspawned."));
            return;
        }
        TryLaunch(global_target, local_target);
    }

    public void TryLaunch(GlobalTargetInfo glo_targ, LocalTargetInfo loc_targ)
    {
        if (!glo_targ.IsValid)
        {
            Log.Error(string.Concat("Tried to launch ", parent, ", but pod lacks valid targeting data."));
            return;
        }
        else
        {
            if (!ReadyForLaunch || !ConnectedToFuelingPort || !FuelingPortSourceHasAnyFuel)
            {
                return;
            }
            Map map = parent.Map;
            int num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, glo_targ.Tile);
            if (num <= MaxLaunchDistance)
            {
                float amount = Mathf.Max(FuelNeededToLaunchAtDist(num), 1f);
                launchGroupFuelingPorts.AsParallel().ForAll(x => x.TryGetComp<CompRefuelable>().ConsumeFuel(amount));
                foreach (ThingWithComps launchable in launchGroup)
                {
                    UtilityPodDef governor = launchable.TryGetComp<CompUtilityLaunchable>().Props.utilityPodDef;//DefDatabase<UtilityPodDef>.GetNamed("JustOneExampleDefName")
                    UtilityPodLeaving cl = (UtilityPodLeaving)SkyfallerMaker.MakeSkyfaller(governor.leavingDef);
                    cl.utilityPodDef = governor;
                    cl.destinationTile = glo_targ.Tile;
                    cl.cell = loc_targ.Cell;
                    launchable.Destroy();
                    GenSpawn.Spawn(cl, launchable.Position, map);
                }
                CameraJumper.TryHideWorld();
            }
        }

    }*/



    public static int MaxLaunchDistanceAtFuelLevel(float fuelLevel)
    {
        return Mathf.FloorToInt(fuelLevel / 2.25f);
    }

    public static float FuelNeededToLaunchAtDist(float dist)
    {
        return 2.25f * dist;
    }

    public void StartChoosingDestination()
    {
        CameraJumper.TryJump(CameraJumper.GetWorldTarget(parent));
        Find.WorldSelector.ClearSelection();
        int tile = parent.Map.Tile;
        Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, canTargetTiles: true, TargeterMouseAttachment, closeWorldTabWhenFinished: true, delegate
        {
            GenDraw.DrawWorldRadiusRing(tile, MaxLaunchDistance);
        }, (GlobalTargetInfo target) => TargetingLabelGetter(target, tile, MaxLaunchDistance, launchGroup));
    }

    private bool ChoseWorldTarget(GlobalTargetInfo target)
    {
        if (!ReadyForLaunch)
        {
            return true;
        }
        return ChoseWorldTarget(target, parent.Map.Tile, launchGroup, MaxLaunchDistance);
    }

    public static bool ChoseWorldTarget(GlobalTargetInfo target, int tile, IEnumerable<ThingWithComps> pods, int maxLaunchDistance)
    {
        if (!target.IsValid)
        {
            Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
        if (maxLaunchDistance > 0 && num > maxLaunchDistance)
        {
            Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        IEnumerable<FloatMenuOption> source = (pods.FirstOrDefault() != null) ? GetOptionsForTile(target, pods) : null;

        if (!source.Any())
        {
            if (Find.World.Impassable(target.Tile))
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return true;
        }
        if (source.Count() == 1)
        {
            if (!source.First().Disabled)
            {
                source.First().action();
                return true;
            }
        }
        //niche case? (possible deprec; keeping for now)
        Find.WindowStack.Add(new FloatMenu(source.ToList()));

        return false;
    }

    public static IEnumerable<FloatMenuOption> GetOptionsForTile(GlobalTargetInfo target, IEnumerable<ThingWithComps>pods)
    {
        bool anything = false;
        List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
        for (int i = 0; i < worldObjects.Count; i++)
        {
            if (worldObjects[i].Tile != target.Tile)
            {
                continue;
            }
            if (worldObjects[i] is MapParent mapParent && mapParent.HasMap)
            {
                anything = true;
                /*yield return new FloatMenuOption("Bombard tile", delegate
                {
                });*/
                yield return new FloatMenuOption("Zoom in to select target.", delegate
                {
                    foreach(ThingWithComps launchable in pods)
                    {
                        launchable.TryGetComp<CompUtilityLaunchable>().global_target = target;
                    }
                    setTargetTile(mapParent, pods);
                });
            }
        }
        if (!anything && !Find.World.Impassable(target.Tile))
        {
            yield return new FloatMenuOption("Pods will have no effect.", delegate
            {
                foreach (ThingWithComps launchable in pods)
                {
                    launchable.TryGetComp<CompUtilityLaunchable>().global_target = target;
                }
            });
        }
    }



    // USER OF OPTION SELECTION FUNCTIONS - result of 'choseworldtarget' input
    public static void setTargetTile(MapParent mp, IEnumerable<ThingWithComps> pods)
    {
        Find.WorldTargeter.StopTargeting();
        Current.Game.CurrentMap = mp.Map;
        Find.CameraDriver.JumpToCurrentMapLoc(mp.Map.Center);
        TargetingParameters targetingParameters = new TargetingParameters();
        targetingParameters.canTargetLocations = true;
        targetingParameters.canTargetBuildings = true;
        targetingParameters.canTargetHumans = true;
        targetingParameters.canTargetAnimals = true;
        targetingParameters.canTargetItems = true;
        targetingParameters.canTargetPawns = true;
        targetingParameters.canTargetSelf = true;

        foreach (ThingWithComps launchable in pods)
        {
            Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo x)
            {
                if (x.IsValid)
                {
                    //Messages.Message("Target destination set.", MessageTypeDefOf.SilentInput, historical: false);
                    Messages.Message(String.Concat("target set for ", launchable.Label,"."), MessageTypeDefOf.NeutralEvent);
                    launchable.TryGetComp<CompUtilityLaunchable>().local_target = x;
                }
            });
        }

        Current.Game.CurrentMap = pods.FirstOrDefault().Map;
        Find.CameraDriver.JumpToCurrentMapLoc(pods.FirstOrDefault().Position);
    }

    

    public static string TargetingLabelGetter(GlobalTargetInfo target, int tile, int maxLaunchDistance, IEnumerable<ThingWithComps> pods)
    {

        if (!target.IsValid)
        {
            return null;
        }
        int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
        if (maxLaunchDistance > 0 && num > maxLaunchDistance)
        {
            GUI.color = ColorLibrary.RedReadable;
            return "TransportPodDestinationBeyondMaximumRange".Translate();
        }

        List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
        for (int i = 0; i < worldObjects.Count; i++)
        {
            if (worldObjects[i].Tile != target.Tile)
            {
                continue;
            }
            String fmtext = "Set target to " + worldObjects[i].Label;
            if (worldObjects[i] is MapParent mapParent && mapParent.HasMap)
            {
                fmtext = fmtext + ": zoom in to select position on map.";
            }
            else if (worldObjects[i].Faction != null)
            {
                if (worldObjects[i].Faction.GoodwillWith(Faction.OfPlayer) > -75)
                {
                    fmtext = fmtext + ": " + pods.Sum(x => x.TryGetComp<CompUtilityLaunchable>().Props.utilityPodDef.goodwillImpact).ToString() + " relation with " + worldObjects[i].Faction.Name;
                }
                fmtext = fmtext + ".";
            }
            return fmtext;

        }
        return null;
    }
}