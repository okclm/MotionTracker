using MelonLoader;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection; 
using System.Collections;
using Il2Cpp;
using MelonLoader.TinyJSON;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Il2CppTLD.Gear;
using UnityEngine.Playables;
using System.Runtime.Intrinsics.X86;
using Il2CppTLD.Logging;
using Il2CppNodeCanvas.Tasks.Actions;
using HarmonyLib;

namespace MotionTracker
{
    // Let's talk Unity events.  https://gamedevbeginner.com/start-vs-awake-in-unity/
    // I think this is referring to events for the base MonoBehaviour object.
    // Awake, OnEnable, Start, FixedUpdate, Update, LateUpdate,OnDisable, and OnDestroy.
    // From the GearItem object inspector, I don't think there is an OnDisable, Start, or FixedUpdate event.
    // But I think there ARE OnDestroy, Awake, CacheComponents, and ManualUpdate events.

    // This is the hooked GearItem Awake method.  Awake is called for each GearItem early and isn't of a lot of value for us as we need more information
    // for each Arrow GearItem (i.e. Is it in a container or player's inventory) that is not populated at this point.
    // The better place for us is the ManualUpdate method (see below).
    //[HarmonyLib.HarmonyPatch(typeof(GearItem), "Awake")]
    //public class GearItemAwakePatch
    //{
    //    public static void Postfix(ref GearItem __instance)
    //    {
    //        // MelonLogger.Msg("[MotionTracker].Harmony.Postfix.50 See " + __instance.name);  // This could be a lot of log data!

    //        if (__instance.gameObject.name.Contains("Arrow"))
    //        {
    //            // Add the Pingcomponent to the Arrow.  The pingComponent updates the position regulary and translates it to the UI

    //            //MelonLogger.Msg("[MotionTracker].Harmony.Postfix.61 See some kind of Arrow (" + __instance.name + ":" + __instance.m_InstanceID + ") and adding PingComponent to object.");
    //            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);
    //        }   // Arrow
    //    }
    //}

    public class MyLogger
    {
        // public static void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null)
        public static void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? filepath = null)

        {
#if DEBUG
            MelonLogger.Msg(Path.GetFileName(filepath) + ":" + caller + "." + lineNumber + ": " + message);
#endif
        }
    }

    // Lost and Found box

    // public unsafe void Awake()
    [HarmonyLib.HarmonyPatch(typeof(Container), "Awake")]
    public class ContainerAwakePatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))      // Limit this to Lost And Found Box containers.
            {
#if DEBUG
                // This is the Awake event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container Awake event.");
#endif
            }
        }
    }

    // public unsafe void Start()
    [HarmonyLib.HarmonyPatch(typeof(Container), "Start")]
    public class ContainerStartPatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))     // Limit this to Lost And Found Box containers.
            {
#if DEBUG
                // This is the Start event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container Start event.");
#endif
            }
        }
    }

    // public unsafe void OnEnable()
    [HarmonyLib.HarmonyPatch(typeof(Container), "OnEnable")]
    public class ContainerOnEnablePatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))     // Limit this to Lost And Found Box containers.
            {
#if DEBUG
                // This is the OnEnable event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnEnable event.");
#endif
            }
        }
    }

    // public unsafe void OnDisable()
    [HarmonyLib.HarmonyPatch(typeof(Container), "OnDisable")]
    public class ContainerOnDisablePatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))     // Limit this to Lost And Found Box containers.
            {
#if DEBUG
                // This is the OnDisable event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnDisable event.");
#endif
                // If the PingComponent exists, delete it.
                if (__instance.gameObject.GetComponent<PingComponent>())
                {
#if DEBUG
                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") PingComponent exists for Lost and Found Box container.  Delete pingComponent to remove from radar.");
#endif
                    PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                }

            }
        }
    }

    // public unsafe void OnDestroy()
    [HarmonyLib.HarmonyPatch(typeof(Container), "OnDestroy")]
    public class ContainerOnDestroyPatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))     // Limit this to Lost And Found Box containers.
            {
#if DEBUG
                // This is the OnDestroy event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnDestroy event.");
#endif
                // If the PingComponent exists, delete it.  
                if (__instance.gameObject.GetComponent<PingComponent>())
                {
#if DEBUG
                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") PingComponent exists for Lost and Found Box container.  Delete pingComponent to remove from radar.");
#endif
                    PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                }

            }
        }
    }

    // public unsafe void UpdateContainer()
    [HarmonyLib.HarmonyPatch(typeof(Container), "UpdateContainer")]
    public class ContainerUpdateContainerPatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))     // Limit this to Lost And Found Box containers.
            {
#if DEBUG
                // This is the UpdateContainer event that is called for each InaccessibleGearContainer.
                // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container UpdateContainer event.");
#endif
                // The Lost and Found Box is active and updating.  Need to add the pingComponent (if not present) to the Lost and Found Box so it shows up on the radar
                if (!__instance.gameObject.GetComponent<PingComponent>())
                {
#if DEBUG
                    //MyLogger.LogMessage("See Lost and Found Box container (" + __instance.name + ":" + __instance.GetInstanceID() + ") at " + __instance.transform.position + 
                    //    " with " + __instance.m_Items.Count + " items and adding PingComponent to object to display on radar.");
#endif
                    __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.LostAndFoundBox);   // Add the PingComponent for the LostAndFoundBox
                }
            }
        }
    }

    //    public unsafe bool TryAddToExistingStackable(GearItem gearToAdd, float normalizedCondition, int numUnits, out GearItem existingGearItem)
    [HarmonyLib.HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.TryAddToExistingStackable), [typeof(GearItem), typeof(float), typeof(int), typeof(GearItem)], [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out])]
    public class PlayerManagerTryAddToExistingStackable
    {
        public static bool Prefix(GearItem gearToAdd, float normalizedCondition, int numUnits, GearItem existingGearItem)
        {
            // This should probably just be for Coal.  The arrow and raw fish stuff seems to play nicely on the radar.  But leave arrow and raw fish in for now.
            if (gearToAdd.gameObject.name.Contains("Arrow") || gearToAdd.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(gearToAdd))
            {
#if DEBUG
                //MyLogger.LogMessage("(" + gearToAdd.DisplayName + ":" + gearToAdd.m_InstanceID + ") TryAddToExistingStackable Prefix(GearItem gearToAdd, float normalizedCondition, int numUnits, out GearItem existingGearItem) event.");
#endif
                // Can we check for a PingComponent component here?
                if (gearToAdd.gameObject.GetComponent<PingComponent>() != null)     // The gear to be added has a PingComponent and it's going into inventory or a container.  So need to delete the PingComponent so it does not display on radar.
                {
#if DEBUG
                    //MyLogger.LogMessage("   (" + gearToAdd.gameObject.name + ":" + gearToAdd.gameObject.GetInstanceID() + ") PingComponent exists for Gear item going into inventory/container.  Delete pingComponent to remove from radar.");
#endif
                    PingComponent.ManualDelete(gearToAdd.gameObject.GetComponent<PingComponent>()); // Delete PingComponent so it no longer shows on radar.
                }

                return true;   // Wild guess.  When set to false, coal does not stack in player inventory.  Let's try true.
            }
            return true;    //  Let's try true.
        }
    }

    // We are relying on the ManualUpdate event to catch GearItems needing to be tracked or not (instantiate a pingComponent or delete an existing pingComponent.
    // In testing, sometimes an Arrow or Coal on the radar becomes "stuck" at the origin (center) that never updates.
    // Hypothesis: This happens after picking up an Arrow or Coal from the ground.  It goes into inventory (Arrow GearItem object is deleted but Coal is not) but
    // the associated radar icon is not deleted.  Suspect pingComponent does not exist.  But the radar is not cleared of the arrow that was picked up and went into
    // inventory.

    [HarmonyLib.HarmonyPatch(typeof(GearItem), "ManualUpdate")]
    public class GearItemManualUpdatePatch
    {
        public static void Postfix(ref GearItem __instance)
        {
            if (__instance.gameObject.name.Contains("Arrow") 
                || __instance.gameObject.name.Contains("Coal") 
                //|| __instance.gameObject.name.Contains("RawCohoSalmon")
                || PingComponent.IsRawFish(__instance)
                )
            {
#if DEBUG
                //MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") inventory=" + __instance.m_InPlayerInventory + ", container=" + __instance.m_InsideContainer + ") manualupdate event.");
#endif
                if (__instance.m_InsideContainer)
                {
#if DEBUG
                    //MyLogger.LogMessage(" (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") is inside a container.");
#endif
                    if (__instance.gameObject)
                    {
#if DEBUG
                        //MyLogger.LogMessage("  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") gameObject exists.");
#endif
                        if (__instance.gameObject.GetComponent<PingComponent>())
                        {
#if DEBUG
                            //MyLogger.LogMessage("   (" + __instance.name + ":" + __instance.m_InstanceID + ") PingComponent exists for Gear item in container.  Delete pingComponent to remove from radar.");
#endif
                            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                        }
                    }
                }
                else if (__instance.m_InPlayerInventory)
                {
#if DEBUG
                    //MyLogger.LogMessage(" (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") is in player inventory.");
#endif
                    if (__instance.gameObject)
                    {
#if DEBUG
                        //MyLogger.LogMessage("  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") gameObject exists.");
#endif
                        if (__instance.gameObject.GetComponent<PingComponent>())
                        {
#if DEBUG
                            //MyLogger.LogMessage("   (" + __instance.name + ":" + __instance.m_InstanceID + ") PingComponent exists for Gear item in inventory.  Delete pingComponent to remove from radar.");
#endif
                            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                        }
                    }
                }
                else
                {
                    if (!__instance.gameObject.GetComponent<PingComponent>())
                    {
                        // Gear item (i.e. Arrow) is not in inventory or container and does not have a PingComponent
#if DEBUG
                        //MyLogger.LogMessage("See some kind of wild Gear item (" + __instance.name + ":" + __instance.m_InstanceID + ") at " + __instance.transform.position + " and adding PingComponent to object to display on radar.");
#endif
                        if (__instance.gameObject.name.Contains("Arrow"))
                        {
                            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);   // Add the PingComponent for the arrow
                        }
                        else if (__instance.gameObject.name.Contains("Coal"))
                        {
                            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Coal);   // Add the PingComponent for the coal
                        }
                        else if (PingComponent.IsRawFish(__instance))  // Need an IsRawFish() bool function. 
                        {
                            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.RawFish);   // Add the PingComponent for a RawFish
                        }
                        __instance.gameObject.GetComponent<PingComponent>().attachedGearItem = __instance;              // Pointer to this GearItem object.
                    }

                }
            }   // Arrow, Coal, or other tracked Gear items.

        }
    }

    // OnDestroy is called when a stacked GearItem is returned to inventory.  It's not called when the item is a single (non-stacked).  Not sure why.
    [HarmonyLib.HarmonyPatch(typeof(GearItem), "OnDestroy")]
    public class GearItemDestroyPatch
    { 
        public static void Postfix(ref GearItem __instance)
        {
#if DEBUG
            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
            {
                //MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") OnDestroy event.");
            }
#endif

            if (__instance.gameObject.GetComponent<PingComponent>())
            {
#if DEBUG
                if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
                {
                    //MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") PingComponent exists.");
                }
#endif

                PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
            }
            else
            {
#if DEBUG
                if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
                {
                    //MyLogger.LogMessage("No PingComponent to delete."); // Lot of logged data so we limit this to justthe GeearItems we are interested in (Arrow, Coal, etc).
                }
#endif
            }
        }
    }
    
    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "Start")]
    public class AiAwakePatch   // This should probably be named AiStartPatch.
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAI Start event.");
#endif

            if (__instance.m_CurrentMode == AiMode.Dead || __instance.m_CurrentMode == AiMode.Disabled || __instance.m_CurrentMode == AiMode.None)
            {
#if DEBUG
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") AiMode dead, disabled, or none.  No processing.");
#endif
                return;
            }

            if (__instance.m_AiSubType == AiSubType.Moose)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Moose);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Bear)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Bear);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Cougar)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Cougar);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Wolf && (__instance.gameObject.name.Contains("grey") || __instance.gameObject.name.Contains("grey")))
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Timberwolf);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Wolf)
            {             
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Wolf);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Stag && !__instance.gameObject.name.Contains("_Doe"))
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Stag);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Stag && __instance.gameObject.name.Contains("_Doe"))
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Doe);
                return;
            }
            else if (__instance.m_SnowImprintType == SnowImprintType.PtarmiganFootprint)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.PuffyBird);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Rabbit)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Rabbit);
                return;
            }
            else if (true)
            {
                return;
            }
        }
    }

    // Crows.  They show up on the radar as expected.  But, sometimes the radar animation for crows stops.  Go into a trailer with active crows on the radar
    // and sometimes the radar crow updates stops.  They should be removed from the radar since there are no crows in the trailer.
    // Also, have active crows on the radar.  Pass time until night.  The crows go away when it's dark.  But the radar shows the non-updating
    // crow artifacts.  UE shows no pingComponents after crows despawn at night.  So, need to figure out how the radar is cleared up when an item despawns.
    // The radar (PingManager) has an iconContainer which contains the radar icons.  The radar icons are Image objects.  The radar icons visibility are updated in the PingManager Update method.
    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "Start")]
    public class FlockPatch
    {
        // public static void Postfix(ref BaseAi __instance)
        public static void Postfix(ref FlockChild __instance)
        {
            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);        // Hmmm... is this the right place to add the PingComponent?
#if DEBUG
            //MyLogger.LogMessage("FlockChild Start event. FlockChild:ID:Position (" + __instance.name + ":" + __instance.GetInstanceID() + ":" + __instance.transform.position + ") " +
            //                    "GameObject:ID:Position (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ":" + __instance.gameObject.transform.position + ")" );
#endif
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "Update")]
    public class FlockUpdatePatch
    {
        // public unsafe virtual void Update()
        public static void Postfix(ref FlockChild __instance)
        {
            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);

#if DEBUG
                //MyLogger.LogMessage("FlockChild Update event. FlockChild:ID:Position (" + __instance.name + ":" + __instance.GetInstanceID() + ":" + __instance.transform.position + ") " +
                //                    "GameObject:ID:Position (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ":" + __instance.gameObject.transform.position + ")");  // Lot of data!
#endif
            }
        }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "Start")]
    public class FlockController_Start_Patch
    {
        // public unsafe virtual void Start()
        public static void Postfix(ref FlockController __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("FlockController:ID (" + __instance.name + ":" + __instance.GetInstanceID() + ") " +
            //                    "GameObject:ID (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ")" +
            //                    " FlockController Start event.");

#endif
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "Update")]
    public class FlockController_Update_Patch
    {
        // public unsafe virtual void Update()
        public static void Postfix(ref FlockController __instance)
        {
            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);
#if DEBUG
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockController Update event.");
#endif
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "destroyBirds")]
    public class FlockController_destroyBirds_Patch
    {
        // public unsafe virtual void destroyBirds()
        public static void Postfix(ref FlockController __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockController destroyBirds event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "EnterDead")]
    public class DeathPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi EnterDead event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());           
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "OnDisable")]
    public class DeathPatch2
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi OnDisable event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    // Despawn not seen yet.  Leave it for now.
    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "Despawn")]
    public class DeathPatch3
    {
        //     public unsafe void Despawn()
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi Despawn event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "ProcessDead")]
    public class ProcessDeadPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi ProcessDead event.");    // Lot of data!
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "ExitDead")]
    public class ExitDeadPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi ExitDead event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }


    [HarmonyLib.HarmonyPatch(typeof(Panel_Base), "Enable", new Type[] { typeof(bool)})]
    public class PanelPatch
    {
        public static void Postfix(ref Panel_Base __instance, bool enable)
        {
#if DEBUG
            // MyLogger.LogMessage("Panel_Base enabled.");
#endif
            PingManager.inMenu = enable;
        }
    }
   

    [HarmonyLib.HarmonyPatch(typeof(DynamicDecalsManager), "TrySpawnDecalObject", new Type[] { typeof(DecalProjectorInstance) })]
    public class TrySpawnDecalObjectPatch
    {
        public static void Postfix(ref DynamicDecalsManager __instance, ref DecalProjectorInstance decalInstance)
        {
            if (decalInstance.m_DecalProjectorType == DecalProjectorType.SprayPaint)
                {
                    Vector3 position;
                    Quaternion rotation;
                    Vector3 vector;
                    __instance.CalculateDecalTransform(decalInstance, null, out position, out rotation, out vector);

                    GameObject decalContainer = new GameObject("DecalContainer");
                    decalContainer.transform.position = position;
                    decalContainer.transform.rotation = rotation;

                    decalContainer.AddComponent<PingComponent>().Initialize(decalInstance.m_ProjectileType);
                }
        }
    }

   

}
