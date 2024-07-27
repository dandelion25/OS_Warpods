using System;
using System.Text.RegularExpressions;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace OS_WarPods;

public class UtilityPodLeaving : Skyfaller, IActiveDropPod, IThingHolder
{
    public int destinationTile = -1;

    public IntVec3 cell = new IntVec3(0, 0, 0);

    public bool createWorldObject = true;

    //public worldObjectDef

    public UtilityPodDef utilityPodDef;

    public Action<UtilityPodDef> onUtilityPodImpact;

    public Action<UtilityPodDef> onUtilityPodDeploy;


    public ActiveDropPodInfo Contents
    {
        get
        {
            return ((ActiveDropPod)innerContainer[0]).Contents;
        }
        set
        {
            ((ActiveDropPod)innerContainer[0]).Contents = value;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref utilityPodDef, "leavingDef");
        Scribe_Values.Look(ref destinationTile, "destinationTile", 0);
        Scribe_Values.Look(ref createWorldObject, "createWorldObject", defaultValue: true);
        /*Scribe_Values.Look(ref onUtilityPodDeploy, "onUtilityPodDeploy");
        Scribe_Values.Look(ref onUtilityPodImpact, "onUtilityPodImpact");*/
    }

    protected override void LeaveMap()
    {
        if (!createWorldObject)
        {
            base.LeaveMap();
            return;
        }
        UtilityPodWorldObject travelingPod = (UtilityPodWorldObject)WorldObjectMaker.MakeWorldObject(utilityPodDef.travelingDef);
        travelingPod.Tile = base.Map.Tile;
        travelingPod.SetFaction(Faction.OfPlayer);
        travelingPod.destinationTile = destinationTile;
        travelingPod.utilityPodDef = utilityPodDef;
        travelingPod.cell = cell;
        travelingPod.onUtilityPodImpact = onUtilityPodImpact;
        travelingPod.onUtilityPodDeploy = onUtilityPodDeploy;
        Find.WorldObjects.Add(travelingPod);
        base.LeaveMap();
    }
}