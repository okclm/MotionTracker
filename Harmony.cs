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
using Il2CppTLD.Gameplay;
using static Il2CppRewired.Demos.SimpleControlRemapping;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime;    // (Unity 6 fix).  Allows us to use Il2CppType.Of<T>() to replace generic GetComponent<T>() calls that break under Unity 6's IL2CppInterop.
using UnityEngine.SceneManagement;

//
// This project is version 1.2.0 from okclm that is being updated to 1.5.0 to work under TLD 2.5+ / Unity 6.
// Note: MotionTracker 1.2.0 was updated by okclm to:
//    - Added Cougar
//    - Fixed issue with Crows remaining on the radar after they were deleted or the scene changed.
//    - Added Salt Deposits (Harvestable objects)
//    - Added Arrows, Arrows heads, and Arrow shafts (GearItem objects)
//    - Added Beachcombing Loot (TideLineSpawners and TideSpawners)
//    - Lost and Found boxes
// MotionTracker 1.2.0 was never released as an update to 1.1.0.

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

    class SpawnUtils
    {
        internal static List<GameObject> GetRootObjects()
        {
            List<GameObject> rootObj = new List<GameObject>();

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                MyLogger.LogMessage("SpawnUtils GetRootObjects: Scene (" + scene.name +  ").");

                GameObject[] sceneObj = scene.GetRootGameObjects();

                foreach (GameObject obj in sceneObj)
                {
                    rootObj.Add(obj);

                    MyLogger.LogMessage("SpawnUtils GetRootObjects: (" + obj.name + ":" + obj.GetInstanceID() + ") at [" + obj.transform.position + "].");
                }
            }

            return rootObj;
        }

        internal static void GetChildren(GameObject obj, List<GameObject> result)
        {
            if (obj.transform.childCount > 0)
            {

                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    GameObject child = obj.transform.GetChild(i).gameObject;
                    result.Add(child);

                    MyLogger.LogMessage("SpawnUtils GetChildren: (" + child.name + ":" + child.GetInstanceID() + ") at [" + child.transform.position + "] activeSelf=" + child.activeSelf + ".");

                    GetChildren(child, result);
                }
            }
        }
    }

        // Beachcombing Adjustments Mod
        // https://github.com/ds5678/BeachcombingAdjustments

        // TODO Track Beachcombing generated treasure. The Update sees the beach spawned items.  Need to see when they are picked up so we can delete the pingComponent.
        // The challenge is that the spawned items are a variety of objects we are not not neccessiarily tracking.  Like Gear_Stick.  But if you pick up the tide spawned Gear_Stick, it needs the PingComponent deleted.

        // Nope.  Spawned items not updated at this point.  The spawned items are not updated until the BeachcombingSpawner Update method is called.

    // public unsafe void Awake()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "Awake")]
    public class BeachcombingSpawnerAwakePatch
    {
        public static void Postfix(ref BeachcombingSpawner __instance)
        {
            MyLogger.LogMessage("!!BeachcombingSpawner Awake event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with " + __instance.m_ChildSpawners.Count + " child spawners.");
        }
    }

    // The spawned items are updated when the BeachcombingSpawner Update method is called.  BUT, TOO MUCH LOG DATA!  Need a throttle.
    // Our throttle is to limit the log data to only TideLineSpawners or TideSpawners.  Then, we only log the spawned items where we haven't added a PingComponent yet.
    // public unsafe void Update()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "Update")]
    public class BeachcombingSpawnerUpdatePatch
    {
        static float timer = 0f;           // Accumulate the time since last frame so we can do things after the trigger duration is elapsed (triggerTime).
        static float triggerTime = 5f;     // Trigger duration.  When the acculated frame time exceeds this value, we do stuff and reset the timer to zero.
        static bool doOnce = false;        // This is a one time event to get the root objects and children.  Set to True to get the root objects and children once.

        public static void Postfix(ref BeachcombingSpawner __instance)
        {
            timer += Time.deltaTime;    // Accumulated time since we last logged stuff

            if (timer > triggerTime)
            {
                if (doOnce)
                {
                    MyLogger.LogMessage("BeachcombingSpawner Update event: Begin root objects identification.");

                    //Get list of all root objects
                    List<GameObject> rObjs = SpawnUtils.GetRootObjects();
                    foreach (GameObject rootObj in rObjs)
                    {
                        List<GameObject> children = new List<GameObject>();

                        SpawnUtils.GetChildren(rootObj, children);
                    }

                    MyLogger.LogMessage("BeachcombingSpawner Update event: End root objects identification.");

                    doOnce = false;
                }

                if (__instance.name.Contains("Tide"))      // Limit this to TideLineSpawners or TideSpawners.
                {
                    //MyLogger.LogMessage("BeachcombingSpawner Update event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with " + __instance.m_ChildSpawners.Count + " child spawners.");

                    int i = 0;
                    foreach (RadialObjectSpawner ros in __instance.m_ChildSpawners)
                    {
                        //MyLogger.LogMessage("BeachcombingSpawner Update event: (" + __instance.name + ":" + __instance.GetInstanceID()
                        //    + ") with RadialObjectSpawner #" + i + " (" + ros.name + ":" + ros.GetInstanceID() + ")"
                        //    + " with " + ros.m_SplineSamplePoints.Count + " spline sample points.");

                        // Spawn spline points
                        //int iii = 0;
                        //foreach (Vector3 vector3 in ros.m_SplineSamplePoints)
                        //{
                        //    MyLogger.LogMessage("BeachcombingSpawner Update event: Spline points for (" + __instance.name + ":" + __instance.GetInstanceID()
                        //        + ") with RadialObjectSpawner #" + i + " (" + ros.name + ":" + ros.GetInstanceID() + ")"
                        //        + " with spline sample point # " + iii + " at [" + vector3.x + "," + vector3.y + "," + vector3.z + "]");
                        //    iii += 1;
                        //}

                        // Spawned items
                        int ii = 0;
                        foreach (GameObject go in ros.m_Spawns)
                        {
                            //MyLogger.LogMessage("BeachcombingSpawner Update event: Spawned items for (" + __instance.name + ":" + __instance.GetInstanceID() + ") with RadialObjectSpawner #" + i + " (" + ros.name + ") with spawn #" + ii + " (" + go.name + ":" + go.GetInstanceID() + ") at " + go.transform.position + ").");

                            // Let's add a "beachloot" PingComponent to the spawned items.  This will allow us to track the spawned items on radar.
                            if (!go.gameObject.GetComponent<PingComponent>())
                            {
                                //MyLogger.LogMessage("BeachcombingSpawner Update event: See Spawned Beach Loot (" + __instance.name + ":" + __instance.GetInstanceID() + ") with RadialObjectSpawner #" + i + " (" + ros.name + ") with spawn #" + ii
                                //    + " (" + go.name + ":" + go.GetInstanceID() + ") GameObject.ActiveSelf=" + go.gameObject.activeSelf  
                                //    + " at [" + go.transform.position + "] and adding PingComponent to object to display on radar.");
                                ////MyLogger.LogMessage("BeachcombingSpawner Update event: See Spawned Beach Loot (" + go.name + ":" + go.GetInstanceID() + ") at [" + go.transform.position + "] and adding PingComponent to object to display on radar.");

                                go.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.BeachLoot);   // Add the PingComponent for the Beach Loot
                            }
                            ii += 1;
                        }

                        i += 1;
                    }
                }
                // Reset the accumulated time
                timer = 0f;
            }
        }
    }

    //     public unsafe void UpdateBeachcombing()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "UpdateBeachcombing")]
    public class BeachcombingSpawnerUpdateBeachcombingPatch
    {
        public static void Postfix(ref BeachcombingSpawner __instance)
        {
            MyLogger.LogMessage("!!BeachcombingSpawner UpdateBeachcombing event: (" + __instance.name + ":" + __instance.GetInstanceID()
                + ") at [" + __instance.transform.position.x + "," + __instance.transform.position.y + "," + __instance.transform.position.z + "].");
        }
    }

    //     public unsafe void UpdateBigItems()
    // Big items are row boats, human corpse, lockers, planks, etc?
    // Lot of data!
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "UpdateBigItems")]
    public class BeachcombingSpawnerUpdateBigItemsPatch
    {
        static float timer = 0f;           // Accumulate the time since last frame so we can do things after the trigger duration is elapsed (triggerTime).
        static float triggerTime = 5f;     // Trigger duration.  When the acculated frame time exceeds this value, we do stuff and reset the timer to zero.
        public static void Postfix(ref BeachcombingSpawner __instance)
        {

            timer += Time.deltaTime;    // Accumulated time since we last logged stuff

            if (timer > triggerTime)
            {
                //MyLogger.LogMessage("BeachcombingSpawner UpdateBigItems event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");

                // Iterate over the big item locations and check if the big item is spawned.
                int i = 0;
                foreach (BeachcombingBigItemLocation bbil in __instance.m_BigItemLocations)
                {
                    //MyLogger.LogMessage("BeachcombingSpawner UpdateBigItems event: (" + __instance.name + ":" + __instance.GetInstanceID() + "). "
                    //    + "See BeachcombingBigItemLocation #" + i + " (" + bbil.name + ":" + bbil.GetInstanceID() 
                    //    + ") at [" + bbil.transform.position.x + "," + bbil.transform.position.y + "," + bbil.transform.position.z + "].");

                    // BeachcombingBigItemLocation spawned items (GearItem)

                    // Get all GearItem components in the BeachcombingBigItemLocation children.
                    GearItem[] gearItems = bbil.GetComponentsInChildren<GearItem>();

                    // Iterate over the array of BeachcombingBigItemLocation spawned GearItems
                    int ii = 0;
                    foreach (GearItem gearItem in gearItems)
                    {
                        //MyLogger.LogMessage("BeachcombingSpawner UpdateBigItems event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").  "
                        //    + "See BeachcombingBigItemLocation #" + i + " (" + bbil.name + ":" + bbil.GetInstanceID()
                        //    + ") at [" + bbil.transform.position.x + "," + bbil.transform.position.y + "," + bbil.transform.position.z 
                        //    + "] with bbil.activeInHierarchy=" + bbil.gameObject.activeInHierarchy + " and see "
                        //    + "spawned GearItem #" + ii + " (" + gearItem.name + ":" + gearItem.GetInstanceID() + ") gi.activeInHierarchy=" + gearItem.gameObject.activeInHierarchy
                        //    + ", at [" + gearItem.transform.position.x + "," + gearItem.transform.position.y + "," + gearItem.transform.position.z + "].");

                        // Let's add a "beachloot" PingComponent to the spawned items.  This will allow us to track the spawned items on radar.
                        // TODO If the BigItem spawn is an item that can also spawn via the radial spawner, we need to check if the PingComponent exists and delete it.
                        // And then add the Beach Loot PingComponent.  Examples include Raw Coho Salmon, Arrows, etc.
                        // This way the beachloot item shows as a beach loot icon on the radar and not the indivual item icon.  i.e. Treasure Chest vs Fish.

                        PingComponent pingComponent = gearItem.gameObject.GetComponent<PingComponent>();
                        if (pingComponent)
                        {   // A PingComponent exists.

                            // if the GearItem or BigItemLocation is not active, and we have a PingComponent, delete the PingComponent.
//                            if ((gearItem.gameObject.activeInHierarchy == false) || (bbil.gameObject.activeInHierarchy == false))
//                            {
//#if DEBUG
//                                MyLogger.LogMessage("BeachcombingSpawner UpdateBigItems event: See Spawned Beach Loot (" + gearItem.name + ":" + gearItem.GetInstanceID() + ") at [" + gearItem.transform.position + "] and activeInHierarchy is False.  Delete the PingComponent to remove from radar.");
//#endif
//                                PingComponent.ManualDelete(pingComponent);
//                            }
//                            else 
                            if (pingComponent.animalType == PingManager.AnimalType.BeachLoot)
                            {
                                // The PingComponent exists and it is a Beach Loot PingComponent.  Do nothing.

                                //MyLogger.LogMessage("BeachcombingSpawner UpdateBigItems event: See Spawned Beach Loot (" + gearItem.name + ":" + gearItem.GetInstanceID() + ") at [" + gearItem.transform.position + "] and existing BeachLoot PingComponent.");
                            }
                            else
                            {   // The PingComponent exists but it is not a Beach Loot PingComponent.  Delete the existing PingComponent and add the Beach Loot PingComponent.

                                //MyLogger.LogMessage("BeachcombingSpawner UpdateBigItems event: See Spawned Beach Loot (" + gearItem.name + ":" + gearItem.GetInstanceID() + ") at [" + gearItem.transform.position + "] and existing non-BeachLoot PingComponent (" + pingComponent.animalType + ").  Delete the non-BeachLoot PingComponent and add a BeachLoot PingComponent.");

                                PingComponent.ManualDelete(pingComponent);

                                //MyLogger.LogMessage("BeachcombingSpawner UpdateBigItems event: See Spawned Beach Loot (" + gearItem.name + ":" + gearItem.GetInstanceID() + ") at [" + gearItem.transform.position + "] and adding BeachLoot PingComponent to object to display on radar.");

                                gearItem.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.BeachLoot);   // Add the PingComponent for the Beach Loot
                            }
                        }
                        else
                        {   // No PingComponent exists.

                            // Only add a PingComponent if the GearItem and BigItemLocation is active.
                            if ((gearItem.gameObject.activeInHierarchy) && (bbil.gameObject.activeInHierarchy))
                            {
                                //MyLogger.LogMessage("BeachcombingSpawner UpdateBigItems event: See Spawned Beach Loot (" + gearItem.name + ":" + gearItem.GetInstanceID() + ") at [" + gearItem.transform.position + "] and adding BeachLoot PingComponent to object to display on radar.");

                                gearItem.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.BeachLoot);   // Add the PingComponent for the Beach Loot
                            }
                        }
                        
                        ii += 1; // Next spawned item...
                    }

                    i += 1; // Next bbil...
                }
                // Reset the accumulated time
                timer = 0f;
            }
        }
    }

    //     public unsafe void OnValidate()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "OnValidate")]
    public class BeachcombingSpawnerOnValidatePatch
    {
        public static void Postfix(ref BeachcombingSpawner __instance)
        {
            MyLogger.LogMessage("!!BeachcombingSpawner OnValidate event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with GameObject.activeSelf=" + __instance.gameObject.activeSelf + ".");
        }
    }

    //     public unsafe void OnDestroy()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "OnDestroy")]
    public class BeachcombingSpawnerOnDestroyPatch
    {
        public static void Prefix(ref BeachcombingSpawner __instance)
        {
            MyLogger.LogMessage("!!BeachcombingSpawner OnDestroy event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with GameObject.activeSelf=" + __instance.gameObject.activeSelf + ".");
        }
    }

    // Nope.  Spawned items not updated at this point.  The spawned items are not updated until the BeachcombingSpawner Update method is called.
    //     public unsafe void Deserialize(BeachcombingSpawnerSaveData saveData)
//    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "Deserialize")]
//    public class BeachcombingSpawnerDeserializePatch
//    {
//        public static void Postfix(ref BeachcombingSpawner __instance)
//        {
//#if DEBUG
//            MyLogger.LogMessage("!!BeachcombingSpawner Deserialize event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with GameObject.activeSelf=" + __instance.gameObject.activeSelf + ".");
//#endif
//        }
//    }

    //     public unsafe void ExpireBigItems()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "ExpireBigItems")]
    public class BeachcombingSpawnerExpireBigItemsPatch
    {
        public static void Prefix(ref BeachcombingSpawner __instance)
        {
            MyLogger.LogMessage("!!BeachcombingSpawner ExpireBigItems event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with GameObject.activeSelf=" + __instance.gameObject.activeSelf + ".");
        }
    }

    //     public unsafe void DeserializeBigItems()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "DeserializeBigItems")]
    public class BeachcombingSpawnerDeserializeBigItemsPatch
    {
        public static void Prefix(ref BeachcombingSpawner __instance)
        {
            MyLogger.LogMessage("!!BeachcombingSpawner DeserializeBigItems event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with GameObject.activeSelf=" + __instance.gameObject.activeSelf + ".");
        }
    }

    // public unsafe void DrawAndPlaceBigItems()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "DrawAndPlaceBigItems")]
    public class BeachcombingSpawnerDrawAndPlaceBigItemsPatch
    {
        public static void Prefix(ref BeachcombingSpawner __instance)
        {
            MyLogger.LogMessage("!!BeachcombingSpawner DrawAndPlaceBigItems event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with GameObject.activeSelf=" + __instance.gameObject.activeSelf + ".");
        }
    }

    // Lot of data.  Called each frame.
    // public unsafe void CheckForNewBlizzard()
    [HarmonyLib.HarmonyPatch(typeof(BeachcombingSpawner), "CheckForNewBlizzard")]
    public class BeachcombingSpawnerCheckForNewBlizzardPatch
    {
        public static void Prefix(ref BeachcombingSpawner __instance)
        {
            //MyLogger.LogMessage("!!BeachcombingSpawner CheckForNewBlizzard event: (" + __instance.name + ":" + __instance.GetInstanceID() + ") with GameObject.activeSelf=" + __instance.gameObject.activeSelf + ".");
        }
    }


    // ======= RadialObjectSpawner Stuff ========

    // Lot of data?
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "OnDestroy")]
    public class RadialObjectSpawnerOnDestroyPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            //MyLogger.LogMessage("RadialObjectSpawner OnDestroy event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // Lot of data?
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "Awake")]
    public class RadialObjectSpawnerAwakePatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            //MyLogger.LogMessage("RadialObjectSpawner Awake event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // Lot of data. All sticks, stones, branches.
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "SpawnAttemptAllNoVisChecks")]
    public class RadialObjectSpawnerSpawnAttemptAllNoVisChecksPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            //MyLogger.LogMessage("RadialObjectSpawner SpawnAttemptAllNoVisChecks event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // Lot of data.
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "SpawnAttemptOnceWithVisCheck")]
    public class RadialObjectSpawnerSpawnAttemptOnceWithVisCheckPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            //MyLogger.LogMessage("RadialObjectSpawner SpawnAttemptOnceWithVisCheck event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // Lot of data. All sticks, stones, branches.
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "ReleaseSpawnedObjectsToPool")]
    public class RadialObjectSpawnerReleaseSpawnedObjectsToPoolPatch
    {
        static float timer = 0f;           // Accumulate the time since last frame so we can do things after the trigger duration is elapsed (triggerTime).
        //static float triggerTime = 5f;     // Trigger duration.  When the acculated frame time exceeds this value, we do stuff and reset the timer to zero.

        // Need to process this every time so we can delete the PingComponent for the spawned items if they exist.  If we use 5 seconds, we can the miss the chance.
        static float triggerTime = 0f;     // Trigger duration.  When the acculated frame time exceeds this value, we do stuff and reset the timer to zero.
        public static void Prefix(ref RadialObjectSpawner __instance)
        {
            timer += Time.deltaTime;    // Accumulated time since we last logged stuff

            if (timer > triggerTime)
            {
                //MyLogger.LogMessage("RadialObjectSpawner ReleaseSpawnedObjectsToPool event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");

                // Spawned items
                int ii = 0;
                foreach (GameObject go in __instance.m_Spawns)
                {
                    //MyLogger.LogMessage("RadialObjectSpawner ReleaseSpawnedObjectsToPool event: Spawned items for (" + __instance.name + ":" + __instance.GetInstanceID() +  " with spawned object #" + ii + " (" + go.name + ":" + go.GetInstanceID() + ") at " + go.transform.position + ").");

                    // Let's remove the beach loot PingComponent if it exists.  This will remove the spawned item from the radar.
                    if (go.gameObject.GetComponent<PingComponent>())
                    {
                        //MyLogger.LogMessage("RadialObjectSpawner ReleaseSpawnedObjectsToPool event: See RadialObjectSpawner (" + __instance.name + ":" + __instance.GetInstanceID() + ") with spawned object #" + ii
                        //    + " (" + go.name + ":" + go.GetInstanceID() + ") at [" + go.transform.position + "].  PingComponent exists for beach loot.  Delete PingComponent to remove from radar.");

                        PingComponent.ManualDelete(go.gameObject.GetComponent<PingComponent>());
                    }
                    ii += 1;
                }

                // Reset the accumulated time
                timer = 0f;
            }
        }
    }

    // Lot of data?
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "DisableSplineMeshUpdating")]
    public class RadialObjectSpawnerDisableSplineMeshUpdatingPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            // MyLogger.LogMessage("RadialObjectSpawner DisableSplineMeshUpdating event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // Lot of data?
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "SetObjectToSpawnBoundingRadius")]
    public class RadialObjectSpawnerSetObjectToSpawnBoundingRadiusPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            // MyLogger.LogMessage("RadialObjectSpawner SetObjectToSpawnBoundingRadius event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // Lot of data?
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "SetSplineBoundingRadius")]
    public class RadialObjectSpawnerSetSplineBoundingRadiusPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            MyLogger.LogMessage("RadialObjectSpawner SetSplineBoundingRadius event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // Lot of data!  Every stick, stone, branch spawned in the game.
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "RollRandomNumToSpawn")]
    public class RadialObjectSpawnerRollRandomNumToSpawnPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            //MyLogger.LogMessage("RadialObjectSpawner RollRandomNumToSpawn event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // Lot of data!  Every stick, stone, branch spawned in the game.
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "Start")]
    public class RadialObjectSpawnerStartPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance)
        {
            //MyLogger.LogMessage("RadialObjectSpawner Start event: (" + __instance.name + ":" + __instance.GetInstanceID() + ").");
        }
    }

    // public unsafe void RemoveFromSpawns(GameObject go)
    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), nameof(RadialObjectSpawner.RemoveFromSpawns), [typeof(GameObject)], [ArgumentType.Normal])]
    public class RadialObjectSpawnerRemoveFromSpawnsPatch
    {
        public static void Postfix(ref RadialObjectSpawner __instance, GameObject go)
        {
            MyLogger.LogMessage("RadialObjectSpawner RemoveFromSpawns event: Harvesting RadialObjectSpawner (" + __instance.name + ":" + __instance.GetInstanceID()
                + " loot (" + go.name + ":" + go.GetInstanceID() + ") at [" + go.transform.position + "] during.");

            // If the PingComponent exists, delete it.
            if (go.gameObject.GetComponent<PingComponent>())
            {
                MyLogger.LogMessage("RadialObjectSpawner RemoveFromSpawns event: Harvested object (" + go.name + ":" + go.GetInstanceID() + ") PingComponent exists for beach loot.  Delete PingComponent to remove from radar.");

                PingComponent.ManualDelete(go.gameObject.GetComponent<PingComponent>());
            }
        }
    }


    // Salt deposit

    // The Harvestable class may not be the right way to approach this.  Alternative possible class hooks:
    // RandomSpawnObject Start, Update
    // HarvestableInteraction
    // Update: The Harvestable class is a workable approach.  The Harvestable class is the base class for the Salt Deposit object.

    // Let's patch the Deserialize method of the Harvestable class.  This is called when the Salt Deposit is loaded and values initialized.
    // It is executed *after* the Awake and Start methods.  This is where we need to add/delete the PingComponent to the Salt Deposit so it shows/doesn't show up on the radar.
    //  public unsafe void Deserialize(string text)

    [HarmonyLib.HarmonyPatch(typeof(Harvestable), nameof(Harvestable.Deserialize), [typeof(string)], [ArgumentType.Normal])]
    public class HarvestableDeserializePatch
    {
        public static void Postfix(ref Harvestable __instance, string text)
        {
            if (__instance.name.Contains("SaltDeposit"))      // Limit this to salt deposits.
            {
                if (__instance.IsHarvested())               // Check if the salt deposit is harvested.
                {
                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") located at (" + __instance.gameObject.transform.position + 
                    //                ") Harvestable Deserialize event with string \"" + text + "\".  Already harvested.");
                    // If the PingComponent exists, delete it.
                    if (__instance.gameObject.GetComponent<PingComponent>())
                    {
                        //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") PingComponent exists for the Salt Deposit.  Delete pingComponent to remove from radar.");

                        PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                    }
                }
                else
                {
                    // The Salt Deposit exists.  Need to add the pingComponent (if not present) to the Salt Deposit so it shows up on the radar

                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") located at (" + __instance.gameObject.transform.position +
                    //                    ") with string \"" + text + "\". Harvestable Deserialize event.");

                    if (!__instance.gameObject.GetComponent<PingComponent>())
                    {
                        //MyLogger.LogMessage("See Salt Deposit (" + __instance.name + ":" + __instance.GetInstanceID() + ") at " + __instance.transform.position +
                        //    " and adding PingComponent to object to display on radar.");
                        __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.SaltDeposit);   // Add the PingComponent for the Salt Deposit
                    }
                }
            }
        }
    }


    // public unsafe void Start()
    // The Start method is called when the salt deposit is created.  This is where we need to add the PingComponent to the salt deposit so it shows up on the radar.
    // But there is a problem.  The IsHarvested() method does not reliably report status at this point.  Most likely due to a timing / race condition.
    // So we can add the PingComponent to a salt deposit that is actually harvested.  Which leads to zombie minature
    // salt deposit icons on the radar.  We're using the Harvestable Deserialize method (see above) to check if the salt deposit is harvested or not.
    [HarmonyLib.HarmonyPatch(typeof(Harvestable), "Start")]
    public class SaltStartPatch
    {
        public static void Postfix(ref Harvestable __instance)
        {
            if (__instance.name.Contains("SaltDeposit"))      // Limit this to salt deposits.
            {
                if (__instance.IsHarvested())
                {
                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") located at (" + __instance.gameObject.transform.position + ") Harvestable Start event.  Already harvested.");
                }
                else
                {
                    // The Salt Deposit exists.  Need to add the pingComponent (if not present) to the Salt Deposit so it shows up on the radar
                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") located at (" + __instance.gameObject.transform.position + ") Harvestable Start event.");

                    if (!__instance.gameObject.GetComponent<PingComponent>())
                    {
                        //MyLogger.LogMessage("See Salt Deposit (" + __instance.name + ":" + __instance.GetInstanceID() + ") at " + __instance.transform.position +
                        //    " and adding PingComponent to object to display on radar.");
                        __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.SaltDeposit);   // Add the PingComponent for the Salt Deposit
                    }
                }
            }
        }
    }

    //     public unsafe void OnDestroy()
    // The OnDestroy method is called when the salt deposit is destroyed.  Have seen this when the exiting the game.  Not sure if we need to do anything here.
    [HarmonyLib.HarmonyPatch(typeof(Harvestable), "OnDestroy")]
    public class SaltDestroyPatch
    {
        public static void Postfix(ref Harvestable __instance)
        {
            if (__instance.name.Contains("SaltDeposit"))      // Limit this to salt deposits.
            {
                // This is the OnDestroy event that is called for each Harvestable.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") located at (" + __instance.gameObject.transform.position + ") Harvestable OnDestroy event.");
            }
        }
    }

    //     public unsafe void Harvest()
    // The Harvest method is called when the player harvests the salt deposit.  This is where we need to delete the PingComponent (if it exists) to the salt deposit.
    [HarmonyLib.HarmonyPatch(typeof(Harvestable), "Harvest")]
    public class SaltHarvestPatch
    {
        public static void Postfix(ref Harvestable __instance)
        {
            if (__instance.name.Contains("SaltDeposit"))      // Limit this to salt deposits.
            {
                // This is the Harvest event that is called for each Harvestable.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") located at (" + __instance.gameObject.transform.position + ") Harvestable Harvest event.");

                // If the PingComponent exists, delete it.
                if (__instance.gameObject.GetComponent<PingComponent>())
                {
                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") PingComponent exists for the Salt Deposit.  Delete pingComponent to remove from radar.");
                    PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                }


            }
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
                // This is the Awake event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container Awake event.");
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
                // This is the Start event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container Start event.");
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
                // This is the OnEnable event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnEnable event.");
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
                // This is the OnDisable event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnDisable event.");

                // If the PingComponent exists, delete it.
                if (__instance.gameObject.GetComponent<PingComponent>())
                {
                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") PingComponent exists for Lost and Found Box container.  Delete pingComponent to remove from radar.");
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
                // This is the OnDestroy event that is called for each InaccessibleGearContainer.
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnDestroy event.");

                // If the PingComponent exists, delete it.  
                if (__instance.gameObject.GetComponent<PingComponent>())
                {
                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") PingComponent exists for Lost and Found Box container.  Delete pingComponent to remove from radar.");
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
                // This is the UpdateContainer event that is called for each InaccessibleGearContainer.
                // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container UpdateContainer event.");

                // The Lost and Found Box is active and updating.  Need to add the pingComponent (if not present) to the Lost and Found Box so it shows up on the radar
                if (!__instance.gameObject.GetComponent<PingComponent>())
                {
                    //MyLogger.LogMessage("See Lost and Found Box container (" + __instance.name + ":" + __instance.GetInstanceID() + ") at " + __instance.transform.position + 
                    //    " with " + __instance.m_Items.Count + " items and adding PingComponent to object to display on radar.");

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
            // There are a number of GearItem objects that are spawned in the game that have a PingComponent.  These are usually associated with beach combing spawns.
            // However, these are GearItem objects we don't normally track on the radar.  For example, HookAndLine, FlareGun, BeefJerky, etc.
            // We need to check these "exception" beach combing spawn items that are picked up into the player's inventory and has a PingComponent.
            // If so, we need to delete the PingComponent so it does not show up on the radar.

            PingComponent pingComponent = gearToAdd.gameObject.GetComponent<PingComponent>();
            if (pingComponent)
            {   // A PingComponent exists.  Is it a Beach Loot PingComponent?
                if (pingComponent.animalType == PingManager.AnimalType.BeachLoot)
                {
                    MyLogger.LogMessage("PlayerManager.TryAddToExistingStackable event: See Spawned Beach Loot (" + gearToAdd.name + ":" + gearToAdd.GetInstanceID() + ") at [" + gearToAdd.transform.position + "] and existing BeachLoot PingComponent.");

                    // The Beach Loot PingComponent exists so delete it to remove it from the radar.
                    PingComponent.ManualDelete(pingComponent);
                }
            }

            // This should probably just be for Coal.  The arrow and raw fish stuff seems to play nicely on the radar.  But leave arrow and raw fish in for now.
            if (gearToAdd.gameObject.name.Contains("Arrow") || gearToAdd.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(gearToAdd))
            {
                //MyLogger.LogMessage("(" + gearToAdd.DisplayName + ":" + gearToAdd.m_InstanceID + ") TryAddToExistingStackable Prefix(GearItem gearToAdd, float normalizedCondition, int numUnits, out GearItem existingGearItem) event.");

                // Can we check for a PingComponent component here?
                if (gearToAdd.gameObject.GetComponent<PingComponent>() != null)     // The gear to be added has a PingComponent and it's going into inventory or a container.  So need to delete the PingComponent so it does not display on radar.
                {
                    //MyLogger.LogMessage("   (" + gearToAdd.gameObject.name + ":" + gearToAdd.gameObject.GetInstanceID() + ") PingComponent exists for Gear item going into inventory/container.  Delete pingComponent to remove from radar.");
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
            // Can we check for GearItems that have a PingComponent but the GeartItem is not active?  If so, then the delete PingComponent?
            // There is a situation where a beach combing item is spawned and then inactivated.  The spawned item is seen and a PingComponent is added to it.
            // Then the spawned item is deactivated.  The PingComponent is not deleted.  So the item is not seen but the PingComponent is still there on the radar!
            // Let's see if we can clean up for this situation.
            // Wait... if the GearItem is not active, don't think this code is firing for that object!  Hmmm... Ok.. need to do this where the icons are being checked for staleness.

            //            PingComponent pingComponent = __instance.gameObject.GetComponent<PingComponent>();
            //            if (pingComponent != null)
            //            {
            //                if (pingComponent.animalType == PingManager.AnimalType.BeachLoot)
            //                {

            //#if DEBUG
            //                    MyLogger.LogMessage("GearItem ManualUpdate event: (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") PingComponent exists.");
            //#endif
            //#if DEBUG
            //                    MyLogger.LogMessage("GearItem ManualUpdate event: See Spawned Beach Loot (" + __instance.name + ":" + __instance.GetInstanceID() + ") at [" + __instance.transform.position + "] and existing non-BeachLoot PingComponent.  Will delete the PingComponent.");
            //#endif
            //                    PingComponent.ManualDelete(pingComponent);
            //                }
            //            }

            if (__instance.gameObject.name.Contains("Arrow") 
                || __instance.gameObject.name.Contains("Coal") 
                || PingComponent.IsRawFish(__instance)
                )
            {
                //MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") inventory=" + __instance.m_InPlayerInventory + ", container=" + __instance.m_InsideContainer + ") manualupdate event.");
                if (__instance.m_InsideContainer)
                {
                    //MyLogger.LogMessage(" (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") is inside a container.");
                    if (__instance.gameObject)
                    {
                        //MyLogger.LogMessage("  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") gameObject exists.");
                        if (__instance.gameObject.GetComponent<PingComponent>())
                        {
                            //MyLogger.LogMessage("   (" + __instance.name + ":" + __instance.m_InstanceID + ") PingComponent exists for Gear item in container.  Delete pingComponent to remove from radar.");
                            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                        }
                    }
                }
                else if (__instance.m_InPlayerInventory)
                {
                    //MyLogger.LogMessage(" (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") is in player inventory.");
                    if (__instance.gameObject)
                    {
                        //MyLogger.LogMessage("  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") gameObject exists.");
                        if (__instance.gameObject.GetComponent<PingComponent>())
                        {
                            //MyLogger.LogMessage("   (" + __instance.name + ":" + __instance.m_InstanceID + ") PingComponent exists for Gear item in inventory.  Delete pingComponent to remove from radar.");
                            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                        }
                    }
                }
                else
                {
                    if (!__instance.gameObject.GetComponent<PingComponent>())
                    {
                        // Gear item (i.e. Arrow) is not in inventory or container and does not have a PingComponent
                        //MyLogger.LogMessage("See some kind of wild Gear item (" + __instance.name + ":" + __instance.m_InstanceID + ") at " + __instance.transform.position + " and adding PingComponent to object to display on radar.");
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
            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
            {
                //MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") OnDestroy event.");
            }

            if (__instance.gameObject.GetComponent<PingComponent>())
            {
                if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
                {
                    //MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") PingComponent exists.");
                }

                PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
            }
            else
            {
                if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
                {
                    //MyLogger.LogMessage("No PingComponent to delete."); // Lot of logged data so we limit this to justthe GeearItems we are interested in (Arrow, Coal, etc).
                }
            }
        }
    }

    // In 1.1.0, this was the "Awake" event.  Here it was changed to the "Start" event.
    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "Start")]
    public class AiAwakePatch   // This should probably be named AiStartPatch.
    {
        public static void Postfix(ref BaseAi __instance)
        {
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAI Start event.");

            if (__instance.m_CurrentMode == AiMode.Dead || __instance.m_CurrentMode == AiMode.Disabled || __instance.m_CurrentMode == AiMode.None)
            {
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") AiMode dead, disabled, or none.  No processing.");
                return;
            }

            // In 1.2.0, this next line is not present.
            //PingComponent pingComponent = __instance.gameObject.GetComponent<PingComponent>();
            //
            // If it were present, it would be changed as follows:
            // CHANGED (Unity 6 fix): Was GetComponent<PingComponent>().
            // Generic GetComponent<T>() crashes in Unity 6 when called from a Harmony patch
            // during early scene load — the IL2CppInterop generic method cache isn't ready yet.
            // Fix: use the non-generic overload with Il2CppType.Of<T>() and TryCast<T>() instead.
            // PingComponent pingComponent = __instance.gameObject.GetComponent(Il2CppType.Of<PingComponent>())?.TryCast<PingComponent>();

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
            // else if (__instance.m_AiSubType == AiSubType.Wolf && (__instance.gameObject.name.Contains("grey") || __instance.gameObject.name.Contains("grey")))
            // BUG FIX: Original had name.Contains("grey") || name.Contains("grey") — both sides
            // were identical, so wolf names with capital "Grey" were never matched as Timberwolves.
            // Made the string comparison case-insensitive by converting the string to lower case before checking.
            else if (__instance.m_AiSubType == AiSubType.Wolf && __instance.gameObject.name.ToLower().Contains("grey"))
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
            //MyLogger.LogMessage("FlockChild Start event. FlockChild:ID:Position (" + __instance.name + ":" + __instance.GetInstanceID() + ":" + __instance.transform.position + ") " +
            //                    "GameObject:ID:Position (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ":" + __instance.gameObject.transform.position + ")" );
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "Update")]
    public class FlockUpdatePatch
    {
        // public unsafe virtual void Update()
        public static void Postfix(ref FlockChild __instance)
        {
            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);

                //MyLogger.LogMessage("FlockChild Update event. FlockChild:ID:Position (" + __instance.name + ":" + __instance.GetInstanceID() + ":" + __instance.transform.position + ") " +
                //                    "GameObject:ID:Position (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ":" + __instance.gameObject.transform.position + ")");  // Lot of data!
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "Start")]
    public class FlockController_Start_Patch
    {
        // public unsafe virtual void Start()
        public static void Postfix(ref FlockController __instance)
        {
            //MyLogger.LogMessage("FlockController:ID (" + __instance.name + ":" + __instance.GetInstanceID() + ") " +
            //                    "GameObject:ID (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ")" +
            //                    " FlockController Start event.");

        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "Update")]
    public class FlockController_Update_Patch
    {
        // public unsafe virtual void Update()
        public static void Postfix(ref FlockController __instance)
        {
            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockController Update event.");
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "destroyBirds")]
    public class FlockController_destroyBirds_Patch
    {
        // public unsafe virtual void destroyBirds()
        public static void Postfix(ref FlockController __instance)
        {
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockController destroyBirds event.");
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "EnterDead")]
    public class DeathPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi EnterDead event.");
            // PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
            // CHANGED (Unity 6 fix): Same GetComponent<T>() -> non-generic replacement as above.
            PingComponent.ManualDelete(__instance.gameObject.GetComponent(Il2CppType.Of<PingComponent>())?.TryCast<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "OnDisable")]
    public class DeathPatch2
    {
        public static void Postfix(ref BaseAi __instance)
        {
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi OnDisable event.");
            // PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
            // CHANGED (Unity 6 fix): Same GetComponent<T>() -> non-generic replacement as above.
            PingComponent.ManualDelete(__instance.gameObject.GetComponent(Il2CppType.Of<PingComponent>())?.TryCast<PingComponent>());
        }
    }

    // Despawn not seen yet.  Leave it for now.
    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "Despawn")]
    public class DeathPatch3
    {
        //     public unsafe void Despawn()
        public static void Postfix(ref BaseAi __instance)
        {
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi Despawn event.");
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "ProcessDead")]
    public class ProcessDeadPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi ProcessDead event.");    // Lot of data!
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "ExitDead")]
    public class ExitDeadPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi ExitDead event.");
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }


    [HarmonyLib.HarmonyPatch(typeof(Panel_Base), "Enable", new Type[] { typeof(bool)})]
    public class PanelPatch
    {
        public static void Postfix(ref Panel_Base __instance, bool enable)
        {
            // MyLogger.LogMessage("Panel_Base enabled.");
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
