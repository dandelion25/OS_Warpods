using Verse;
using RimWorld;

namespace OS_WarPods;

public class CompProperties_UtilityLaunchable : CompProperties
{
    public bool requireFuel = true;

    public int fixedLaunchDistanceMax = -1;

    public float forcedMissRadius = 0f;

    public UtilityPodDef utilityPodDef;

    public CompProperties_UtilityLaunchable()
    {
        compClass = typeof(CompUtilityLaunchable);
    }

}
