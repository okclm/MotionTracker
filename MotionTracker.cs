using Il2Cpp;
using Il2CppHoloville.HOTween.Core.Easing;
using Il2CppInterop;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMono;
using Il2CppTLD.Logging;
using MelonLoader;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Il2Cpp.Utils;
using static Il2CppSystem.Net.ServicePointManager;


namespace MotionTracker
{
    public class MotionTrackerMain : MelonMod
	{
        public static AssetBundle? assetBundle;
        public static AssetBundle? assetBundle2;
        public static GameObject? motionTrackerParent;
        public static PingManager? activePingManager;

        public static GameObject? trackerPrefab;
        public static GameObject? trackerObject;

        public static GameObject? modSettingPage;

        public static Dictionary<PingManager.AnimalType, GameObject> animalPingPrefabs = new Dictionary<PingManager.AnimalType, GameObject>();  // The dictionary of animal prefabs is instantiated (again?) in FirstTimeSetup.
        public static Dictionary<ProjectileType, GameObject> spraypaintPingPrefabs = new Dictionary<ProjectileType, GameObject>();

        public static void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? filepath = null)
        {
#if DEBUG
            MelonLogger.Msg(Path.GetFileName(filepath) + ":" + caller + "." + lineNumber + ": " + message);
#endif
        }

        public static void LogError(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? filepath = null)
        {
            MelonLogger.Msg(Path.GetFileName(filepath) + ":" + caller + "." + lineNumber + ": " + message);
        }

        public override void OnInitializeMelon()
        {
            // LogMessage("Initializing Melon.");
            LogMessage("[MotionTracker] Version " + Assembly.GetExecutingAssembly().GetName().Version);

            ClassInjector.RegisterTypeInIl2Cpp<TweenManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PingManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PingComponent>();

            LoadEmbeddedAssetBundle();
            LoadEmbeddedAssetBundle2();

            MotionTracker.Settings.OnLoad();
        }

        // Note: This is a work-around for loading an embedded asset bundle in Unity 6.0+ and avoids a garbage collection issue.
        // It involves copying the embedded asset bundle to a temporary file and then loading it from there.
        public static void LoadEmbeddedAssetBundle()
        {
            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MotionTracker.Resources.motiontracker");
            if (stream == null)
            {
                LogError("stream==null!  Failed to load embedded asset bundle.  Return.");
                return;
            }
            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MotionTracker.Resources.motiontracker");
            if (tempPath == null)
            {
                LogError("tempPath==null!  Failed to create temp path for embedded asset bundle.  Return.");
                return;
            }
            LogMessage("tempPath: " + tempPath);

            using (System.IO.FileStream fs = System.IO.File.Create(tempPath))
            {
                stream.CopyTo(fs);
                LogMessage("Copied embedded asset bundle to temp path.");
            }

            assetBundle = AssetBundle.LoadFromFile(tempPath);
            if (assetBundle == null)
            {
                LogError("assetBundle==null!  Failed to load asset bundle from file.  Return.");
                return;
            }

            try
            {
                System.IO.File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete temp asset bundle file: {ex}");
            }
        }

        public static void LoadEmbeddedAssetBundle2()
        {
            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MotionTracker.Resources.motiontrackerassetbundleprefab");
            if (stream == null)
            {
                LogError("stream==null!  Failed to load embedded asset bundle 2.  Return.");
                return;
            }
            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MotionTracker.Resources.motiontrackerassetbundleprefab");
            if (tempPath == null)
            {
                LogError("tempPath==null!  Failed to create temp path for embedded asset bundle 2.  Return.");
                return;
            }
            LogMessage("tempPath: " + tempPath);

            using (System.IO.FileStream fs = System.IO.File.Create(tempPath))
            {
                stream.CopyTo(fs);
                LogMessage("Copied embedded asset bundle 2 to temp path.");
            }

            assetBundle2 = AssetBundle.LoadFromFile(tempPath);
            if (assetBundle2 == null)
            {
                LogError("assetBundle2==null!  Failed to load asset bundle 2 from file.  Return.");
                return;
            }

            try
            {
                System.IO.File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete temp asset bundle 2 file.: {ex}");
            }
        }

        // Note: This is the pre-Unity 6.0 way of loading an embedded asset bundle.  It causes a garbage collection issue in TLD 2.5+.  
        //public static void LoadEmbeddedAssetBundle()    // Orginal AssetBundle with original prefabs
        //{
        //    LogMessage("Loading embedded asset bundle from memory stream.");
        //    MemoryStream? memoryStream;
        //    Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MotionTracker.Resources.motiontracker");
        //    memoryStream = new MemoryStream((int)stream.Length);
        //    stream.CopyTo(memoryStream);

        //    assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
        //}


        // Note: This is the pre-Unity 6.0 way of loading an embedded asset bundle.  It causes a garbage collection issue in TLD 2.5+.  
        //public static void LoadEmbeddedAssetBundle2()   // Additional AssetBundle with additional prefabs (Cougar, Arrow, Coal, Raw Fish, Lost and Found Box)
        //{
        //    LogMessage("Loading embedded asset bundle 2 from memory stream.");
        //    MemoryStream? memoryStream;
        //    Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MotionTracker.Resources.motiontrackerassetbundleprefab");
        //    memoryStream = new MemoryStream((int)stream.Length);
        //    stream.CopyTo(memoryStream);

        //    assetBundle2 = AssetBundle.LoadFromMemory(memoryStream.ToArray());
        //}

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
            //LogMessage($"Scene {sceneName} with build index {buildIndex} has been loaded.");    // CLM
            if (sceneName.Contains("MainMenu"))
            {
                //SCRIPT_InterfaceManager/_GUI_Common/Camera/Anchor/Panel_OptionsMenu/Pages/ModSettings/GameObject/ScrollPanel/Offset/
                // LogMessage("Scene name containing MainMenu " + sceneName + " was loaded.");

                PingManager.inMenu = true;
                
                FirstTimeSetup();
            }
            else if (sceneName.Contains("SANDBOX") && motionTrackerParent)
            {
                LogMessage("Scene name containing SANDBOX " + sceneName + " was loaded.");

                if (PingManager.instance)
                {
                    PingManager.instance.ClearIcons();
                }
                PingManager.inMenu = false;
            }
            else
            {
                // LogMessage("Non-Menu and Non-Sandbox scene " + sceneName + " was loaded.");

                // This is a scene that doesn't have "MainMenu" or "Sandbox" in the name.
                // The original MotionTracker was focused on animals and spraypaint decals.
                // This scene name could be something like "CanneryTrailerA_DLC01" (the trailer in the BI cannery yard).  And if we have stuff on the radar from the previous scene,
                // we should reset that.
                if (PingManager.instance)
                
                {
                    PingManager.instance.ClearIcons();
                }
                PingManager.inMenu = false;
            }
        }

        public void FirstTimeSetup()
        {
            if (!motionTrackerParent)
            {
                motionTrackerParent = new GameObject("MotionTracker");
                trackerObject = UnityEngine.Object.Instantiate(assetBundle.LoadAsset<GameObject>("MotionTracker"), motionTrackerParent.transform);
                GameObject.DontDestroyOnLoad(motionTrackerParent);

                activePingManager = motionTrackerParent.AddComponent<PingManager>();

                GameObject prefabSafe = new GameObject("PrefabSafe");
                prefabSafe.transform.parent = motionTrackerParent.transform;
                animalPingPrefabs = new Dictionary<PingManager.AnimalType, GameObject>();   // Instantiate (again!?) the dictionary of animal prefabs.
                animalPingPrefabs.Add(PingManager.AnimalType.Crow, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("crow"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Rabbit, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("rabbit"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Wolf, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("wolf"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Timberwolf, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("timberwolf"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Bear, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("bear"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Cougar, GameObject.Instantiate(assetBundle2.LoadAsset<GameObject>("cougar"), prefabSafe.transform));  
                animalPingPrefabs.Add(PingManager.AnimalType.Moose, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("moose"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Stag, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("stag"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Doe, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("doe"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.PuffyBird, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("ptarmigan"), prefabSafe.transform));

                // Note these are additional prefabs from the second asset bundle.
                animalPingPrefabs.Add(PingManager.AnimalType.Arrow, GameObject.Instantiate(assetBundle2.LoadAsset<GameObject>("arrow"), prefabSafe.transform));  
                animalPingPrefabs.Add(PingManager.AnimalType.Coal, GameObject.Instantiate(assetBundle2.LoadAsset<GameObject>("coal"), prefabSafe.transform));  
                animalPingPrefabs.Add(PingManager.AnimalType.RawFish, GameObject.Instantiate(assetBundle2.LoadAsset<GameObject>("rawcohosalmon"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.LostAndFoundBox, GameObject.Instantiate(assetBundle2.LoadAsset<GameObject>("lostandfound"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.SaltDeposit, GameObject.Instantiate(assetBundle2.LoadAsset<GameObject>("saltdeposit"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.BeachLoot, GameObject.Instantiate(assetBundle2.LoadAsset<GameObject>("beachloot"), prefabSafe.transform));

                spraypaintPingPrefabs = new Dictionary<ProjectileType, GameObject>();
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Direction, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Direction"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Clothing, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Clothing"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Danger, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Danger"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_DeadEnd, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_DeadEnd"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Avoid, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Avoid"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_FirstAid, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_FirstAid"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_FoodDrink, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_FoodDrink"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_FireStarting, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_FireStarting"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Hunting, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Hunting"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Materials, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Materials"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Storage, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Storage"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Tools, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Tools"), prefabSafe.transform));

                foreach (KeyValuePair<PingManager.AnimalType, GameObject> singlePrefab in animalPingPrefabs)
                {
                    singlePrefab.Value.active = false;
                }

                foreach (KeyValuePair<ProjectileType, GameObject> singlePrefab in spraypaintPingPrefabs)
                {
                    singlePrefab.Value.active = false;
                }

                GameObject.DontDestroyOnLoad(prefabSafe);
            }
        }

        public static GameObject GetAnimalPrefab(PingManager.AnimalType animalType)
        {  
            return animalPingPrefabs[animalType];
        }

        public static GameObject GetSpraypaintPrefab(ProjectileType pingType)
        {
            return spraypaintPingPrefabs[pingType];
        }

        public override void OnUpdate()
		{
            if (Settings.options == null)
            {
                return;
            }

            if (Settings.options.displayStyle == Settings.DisplayStyle.Toggle)
            {
                if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.toggleKey))
                {
                    if (PingManager.instance)
                    {
                        Settings.toggleBool = !Settings.toggleBool;                       
                    }
                }
            }       
        }
    }
}