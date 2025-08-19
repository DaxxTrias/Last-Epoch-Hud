using Il2Cpp;
using Mod.Game;
using UnityEngine;

namespace Mod.Cheats.ESP
{
    internal class Actors
    {
        //        Alignments: 
        //     
        //        Good: Seems to be players and player pets
        //        Evil: Seems to be enemies
        //        Barrel: Containers that are destructible
        //        HostileNeutral: Seems to be neutral enemies
        //        FriendlyNeutral: Seems to be neutral NPCs
        //        SummonedCorpse: Necromancer summons

        private static string GetActorName(ActorVisuals actor)
        {
            if (actor.isPlayer && actor.UserIdentity != null)
            {
                return actor.UserIdentity.Username;
            }
            else
            {
                var displayInformation = actor.gameObject.GetComponent<ActorDisplayInformation>();

                if (displayInformation != null)
                {
                    return displayInformation.displayName;
                }
            }

            return actor.name;
        }

        public static void GatherActors()
        {
            if (ActorManager.instance == null) return;

            var localPlayer = ObjectManager.GetLocalPlayer();
            if (localPlayer == null) return;

            foreach (var visual in ActorManager.instance.visuals)
            {
                if (!Settings.ShouldDrawNPCAlignment(visual.alignment.name)) continue;

                var color = Drawing.AlignmentToColor(visual.alignment.name);

                foreach (var actor in visual.visuals._list)
                {
                    if (!actor.gameObject.activeInHierarchy) continue;

                    //if (actor.GetComponent<ActorDisplayInformation>() != null && 
                    //    !Settings.ShouldDrawNPCClassification(actor.GetComponent<ActorDisplayInformation>()
                    //    .actorClass)) continue;

                    var actorDisplayInfo = actor.GetComponent<ActorDisplayInformation>();
                    var displayInfo = actor.GetComponent<DisplayInformation>();
                    if (actorDisplayInfo != null)
                    {
                        if (actor.gameObject.GetComponent<LootLizardFleeing>() != null ||
                            actor.gameObject.GetComponentInParent<LootLizardFleeing>() != null ||
                            actor.gameObject.GetComponentInChildren<LootLizardFleeing>() != null)
                        {
                            goto skip1;
                        }
                        if (!Settings.ShouldDrawNPCClassification(actorDisplayInfo.actorClass))
                        {
                            continue;
                        }
                    }
                skip1:
                    float distance = Vector3.Distance(
                        actor.transform.position, localPlayer.transform.position);

                    if (distance >= Settings.drawDistance || actor.dead) continue;

                    var name = GetActorName(actor);
                    var position = actor.GetHealthBarPosition();
                    position.y += 0.5f;

                    ESP.AddLine(localPlayer.transform.position, actor.transform.position, color);
                    //ESP.AddString(name + " (" + distance.ToString("F1") + ")  ", position, color);
                    ESP.AddString(name, position, color);

                }
            }
        }

        public static void OnUpdate()
        {
            if (ObjectManager.HasPlayer())
            {
                GatherActors();
            }
        }
    }
}
