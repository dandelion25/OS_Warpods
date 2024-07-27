using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using System.Text;
using Verse.AI.Group;

namespace OS_WarPods;

public class UtilityPodDef : Def
{
    // DEFS (to be referenced)

    public ThingDef buildingDef;

    public ThingDef leavingDef;

    public ThingDef incomingDef;

    public WorldObjectDef travelingDef;

    // PARAMETERS

    public List<ThingDefCountClass> payload; // to be defined within def (like 'costList' in buildable) and added to Skyfaller.

    public int goodwillImpact = 0;

    public float tileBombardmentValue = 0f; //ratio of explosion tiles to 'pocket map' size

    public DamageDef tileBombardmentDamage => (incomingDef.skyfaller.explosionDamage);

    public HistoryEventDef impactHistoryEventDef;

    public float speed = 1f;

    [NoTranslate]
    public string worldTexture;

    public virtual void OnUtilityPodDeploy(IntVec3 pos, Map map)
    {  //blarg - 
        /*if (deployThing != null)
        {
            GenSpawn.Spawn(deployThing, pos, map);
        }*/
    }

    public virtual void OnUtilityPodImpact(IntVec3 pos, Map map)
    {
    }

    public virtual void OnUtilityPodImpactWorldTile(int pos, UtilityPodWorldObject podObject, WorldObject worldObject = null)
    {
        if (worldObject != null)
        {
            Faction launcherFaction = podObject.Faction;

            if (worldObject.Faction!= null)
            {
                Faction targetFaction = worldObject.Faction;

                if (goodwillImpact != 0 && targetFaction != launcherFaction)
                {
                    if (targetFaction.GoodwillWith(launcherFaction) > -75)
                    {
                        targetFaction.TryAffectGoodwillWith(launcherFaction, goodwillImpact, true, true, impactHistoryEventDef);
                    }

                    /*if (worldObject is Settlement && tileBombardmentDamage != null)
                    {
                        foreach (Pawn collateral in ((Settlement)worldObject).previouslyGeneratedInhabitants)
                        {
                            if (UnityEngine.Random.Range(0f, 1f) < tileBombardmentValue)
                            {
                                if (UnityEngine.Random.Range(0f,1f) < 0.2)
                                {
                                    collateral.PreApplyDamage(ref new DamageInfo(tileBombardmentDamage, 30), false);
                                }
                                else
                                {
                                    if (PawnUtility.isFactionLeader(collateral)) {targetFaction.TryAffectGoodwillWith(Faction.OfPlayer, -50, true, true, impactHistoryEventDef}
                                    collateral.Kill(ref new DamageInfo(tileBombardmentDamage, 30))
                                }
                                
                            }
                        }
                        // Scenario: leader killed by pod is covered by 'Faction.Notify_MemberDied', which is covered by 'Pawn.kill()'
                        
                    }*/ // being overambitious as usual haha

                    // RETALIATION
                    // eventual desired effect of implementation: factions can participate in suborbital exchanges separate from the player
                    // if i can pull it off that'd be so cool
                    if (targetFaction.def.techLevel > TechLevel.Industrial && targetFaction.RelationKindWith(launcherFaction) == FactionRelationKind.Hostile)
                    {

                        UtilityPodWorldObject travelingPod = (UtilityPodWorldObject)WorldObjectMaker.MakeWorldObject(travelingDef);
                        travelingPod.Tile = worldObject.Tile;
                        travelingPod.SetFaction(targetFaction);
                        travelingPod.destinationTile = podObject.originTile;
                        travelingPod.utilityPodDef = this;
                        travelingPod.cell = new IntVec3(0,0,0);

                        // target specific pawn if launched from loaded map
                        Map launcherMap = Find.Maps.Find(x => x.Tile == podObject.originTile);
                        if (launcherMap != default(Map))
                        {
                            // get faction-owned target
                            IEnumerable<Pawn> viableTargets = from pwn in launcherMap.mapPawns.AllHumanlikeSpawned where pwn.Faction == launcherFaction select pwn;
                            travelingPod.cell = viableTargets.RandomElement().Position;
                        }

                        //travelingPod.onUtilityPodImpact = OnUtilityPodDeploy; //what are these for again???
                        //travelingPod.onUtilityPodDeploy = null; //argh
                        Find.WorldObjects.Add(travelingPod);
                        Notify_Retaliation(worldObject, Find.WorldObjects.Settlements.Find(x => x.Tile == podObject.originTile), this, true, worldObject, out bool sentLetter);
                    }
                }
            }

            /*if (worldObject.GetType() == typeof(Site))
            {
                if (worldObject.any)
            }*/
        }
    }



    // should put this in a utility class somewhere... oh well.
    private void Notify_Retaliation (WorldObject retaliator, WorldObject target, UtilityPodDef podtype, bool canSendLetter, GlobalTargetInfo lookTarget, out bool sentLetter)
    {
        if (Current.ProgramState != ProgramState.Playing || target.Faction != Faction.OfPlayer)
        {
            canSendLetter = false;
        }

        sentLetter = false;
        ColoredText.ClearCache();
        String text2 = "Hostile " + podtype.label + " Incoming";
        TaggedString text3 = retaliator.Faction.Name + " has launched a suborbital " + podtype.label + " strike towards " + target.Label + ".";

        if (canSendLetter)
        {
            Find.LetterStack.ReceiveLetter(text2, text3, LetterDefOf.NegativeEvent, lookTarget, retaliator.Faction);
            sentLetter = true;
        }

        if (Current.ProgramState != ProgramState.Playing)
        {
            return;
        }
    }
}
