using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace OS_WarPods;

public class UtilityPodWorldObject : WorldObject, IThingHolder
{
    public int destinationTile = -1;

    private float traveledPct;

    public UtilityPodDef utilityPodDef;

    private bool arrived;

    private int initialTile = -1;

    public int originTile => initialTile;

    public IntVec3 cell = new IntVec3(0, 0, 0);

    private const float TravelSpeed = 0.0001f;

    private Material cachedMaterial;

    public Action<UtilityPodDef> onUtilityPodImpact;

    public Action<UtilityPodDef> onUtilityPodDeploy;


    // WorldObject stuff
    private Vector3 Start => Find.WorldGrid.GetTileCenter(initialTile);

    private Vector3 End => Find.WorldGrid.GetTileCenter(destinationTile);

    public override Vector3 DrawPos => Vector3.Slerp(Start, End, traveledPct);

    public override string Label => utilityPodDef.label;

    public override bool ExpandingIconFlipHorizontal => GenWorldUI.WorldToUIPosition(Start).x > GenWorldUI.WorldToUIPosition(End).x;

    public override Material Material
    {
        get
        {
            if (utilityPodDef.worldTexture.NullOrEmpty())
            {
                return null;
            }
            if (cachedMaterial == null)
            {
                cachedMaterial = MaterialPool.MatFrom(utilityPodDef.worldTexture, ShaderDatabase.WorldOverlayTransparentLit, WorldMaterials.WorldObjectRenderQueue);
            }
            return cachedMaterial;
        }
    }

    private float TraveledPctStepPerTick
    {
        get
        {
            Vector3 start = Start;
            Vector3 end = End;
            if (start == end)
            {
                return 1f;
            }
            float num = GenMath.SphericalDistance(start.normalized, end.normalized);
            if (num == 0f)
            {
                return 1f;
            }
            return 0.0001f / num;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref destinationTile, "destinationTile", 0);
        Scribe_Values.Look(ref arrived, "arrived", defaultValue: false);
        Scribe_Values.Look(ref initialTile, "initialTile", 0);
        Scribe_Values.Look(ref traveledPct, "traveledPct", 0f);
        Scribe_Defs.Look(ref utilityPodDef, "travelingDef");

        Scribe_Values.Look(ref onUtilityPodImpact, "onUtilityPodImpact");
        Scribe_Values.Look(ref onUtilityPodDeploy, "onUtilityPodDeploy");
        //Scribe_References.Look(ref spawnFaction, "spawnFaction");
    }

    public override void PostAdd()
    {
        base.PostAdd();
        initialTile = base.Tile;
    }

    public override void Tick()
    {
        base.Tick();
        traveledPct += TraveledPctStepPerTick;
        if (traveledPct >= 1f)
        {
            traveledPct = 1f;
            Arrived();
        }
    }

    public override void Draw()
    {
        Vector3 drawPos = DrawPos;
        float angle = Find.WorldGrid.GetHeadingFromTo(drawPos, End) - 90f;
        Matrix4x4 matrix4x = Matrix4x4.Rotate(Quaternion.AngleAxis(angle, Vector3.up));
        float num = 0.7f * Find.WorldGrid.averageTileSize;
        float num2 = 0.015f;
        Vector3 normalized = drawPos.normalized;
        Quaternion q = Quaternion.LookRotation(Vector3.Cross(normalized, Vector3.up), normalized);
        Matrix4x4 matrix4x2 = Matrix4x4.TRS(s: new Vector3(num, 1f, num), pos: drawPos + normalized * num2, q: q);
        Graphics.DrawMesh(MeshPool.plane10, matrix4x2 * matrix4x, Material, WorldCameraManager.WorldLayer);
    }

    private void Arrived()
    {
        WorldObject worldObject = Find.World.worldObjects.WorldObjectAt<WorldObject>(destinationTile);
        if (worldObject is MapParent mapParent && mapParent.HasMap)
        {
            mapParent.Map.roofGrid.SetRoof(mapParent.Map.Center, null);
            List<Thing> list = mapParent.Map.thingGrid.ThingsListAt(mapParent.Map.Center);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing.def.IsEdifice())
                {
                    thing.Destroy();
                }
            }
            UtilityPodIncoming ci = (UtilityPodIncoming)SkyfallerMaker.MakeSkyfaller(utilityPodDef.incomingDef);
            ci.cell = cell;
            ci.utilityPodDef = utilityPodDef;
            ci.onUtilityPodDeploy = onUtilityPodDeploy;
            ci.onUtilityPodImpact = onUtilityPodImpact;
            ci.spawnFaction = this.Faction;//spawnFaction;
            GenSpawn.Spawn(ci, cell, mapParent.Map);
        }
        else
        {
            utilityPodDef.OnUtilityPodImpactWorldTile(destinationTile, this, worldObject);
        }
        Destroy();
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return null;
    }
}