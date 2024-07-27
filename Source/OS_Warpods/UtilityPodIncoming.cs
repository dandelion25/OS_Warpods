using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace OS_WarPods;

public class UtilityPodIncoming : Skyfaller, IActiveDropPod, IThingHolder
{
    public IntVec3 cell = new IntVec3(0, 0, 0);

    public UtilityPodDef utilityPodDef;

    public Action<UtilityPodDef> onUtilityPodImpact;

    public Action<UtilityPodDef> onUtilityPodDeploy;

    public Faction spawnFaction;

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
        Scribe_Defs.Look(ref utilityPodDef, "incomingDef");
        Scribe_References.Look(ref spawnFaction, "spawnFaction");
    }

    public override void Tick()
    {
        base.Tick();
    }

    // IMPACT BEHAVIOUR
    /*protected override void Impact()
    {
        CrashedPod cp = (CrashedPod)GenSpawn.Spawn(utilityPodDef.crashedDef, base.Position, base.Map, WipeMode.FullRefund);
        cp.onUtilityPodDeploy = onUtilityPodDeploy;
        cp.utilityPodDef = utilityPodDef;
        Destroy();
    }*/

    protected override void SpawnThings()
    {
        // generate inventory on landing based on utilityPodDef.payload ; a b  i  t messy, but maybe it's neater???
        for (int i = 0; i < utilityPodDef.payload.Count; i++)
        {
            ThingDef itemdef = utilityPodDef.payload[i].thingDef;
            int itemcount = utilityPodDef.payload[i].count;
            Thing item = GenSpawn.Spawn(itemdef, new IntVec3(0,0,0), null);
            if (itemdef.CanHaveFaction)
            {
                item.SetFaction(spawnFaction);
            }
            innerContainer.TryAddOrTransfer(item, itemcount, itemcount < itemdef.stackLimit);
        }

        base.SpawnThings();
    }

    protected override void Impact()
    {
        base.Impact();

    }
}
