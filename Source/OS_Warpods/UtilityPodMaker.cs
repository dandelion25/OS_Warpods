using RimWorld;
using UnityEngine;
using Verse;

namespace OS_WarPods;

public static class UtilityPodMaker
{
    public static void MPrint(string str)
    {
        Messages.Message(str, MessageTypeDefOf.NeutralEvent);
    }

    public static UtilityPodLeaving SpawnLeaving(UtilityPodDef c, IntVec3 pos, Map map, int destTile)
    {
        if (!pos.InBounds(map))
        {
            return null;
        }
        UtilityPodLeaving cl = (UtilityPodLeaving)SkyfallerMaker.MakeSkyfaller(c.leavingDef);
        cl.utilityPodDef = c;
        cl.destinationTile = destTile;
        //c.PreLeavingMissileSpawn(ml);
        GenSpawn.Spawn(cl, pos, map);
        return cl;
    }

    public static UtilityPodLeaving SpawnLeaving(UtilityPodDef c, IntVec3 pos, Map map, int destTile, IntVec3 cell)
    {
        if (!pos.InBounds(map))
        {
            return null;
        }
        UtilityPodLeaving cl = (UtilityPodLeaving)SkyfallerMaker.MakeSkyfaller(c.leavingDef);
        cl.utilityPodDef = c;
        cl.destinationTile = destTile;
        cl.cell = cell;
        //c.PreLeavingMissileSpawn(cl);
        GenSpawn.Spawn(cl, pos, map);
        return cl;
    }

    public static UtilityPodIncoming SpawnIncoming(UtilityPodDef c, IntVec3 pos, Map map)
    {
        if (!pos.InBounds(map))
        {
            return null;
        }
        UtilityPodIncoming ci = (UtilityPodIncoming)SkyfallerMaker.MakeSkyfaller(c.incomingDef);
        ci.utilityPodDef = c;
        //ci = c.PreIncomingMissileSpawn(new Position(pos, map), ci);
        GenSpawn.Spawn(ci, pos, map);
        //c.PostIncomingMissileSpawn(new Position(pos, map), ci);
        return ci;
    }

    /*public static Thing SpawnDeployed(UtilityPodDef c, IntVec3 pos, Map map)
    {
        if (!pos.InBounds(map))
        {
            return null;
        }
        CrashedMissile cm = (CrashedMissile)GenSpawn.Spawn(c.crashedDef, pos, map, WipeMode.FullRefund);
        return cm;
    }*/

}
