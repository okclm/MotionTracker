using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection;
using System.Collections;
using Il2Cpp;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Il2CppSystem.Net.ServicePointManager;


namespace MotionTracker
{
    public class MotionTrackerMain : MelonMod
	{
        public static AssetBundle assetBundle;
        public static AssetBundle assetBundle2;
        public static GameObject motionTrackerParent;
        public static PingManager activePingManager;

        public static GameObject trackerPrefab;
        public static GameObject trackerObject;

        public static GameObject modSettingPage;

        public static Dictionary<PingManager.AnimalType, GameObject> animalPingPrefabs = new Dictionary<PingManager.AnimalType, GameObject>();  // The dictionary of animal prefabs is instantiated (again!?) in FirstTimeSetup.
        // public static Dictionary<PingManager.AnimalType, GameObject>? animalPingPrefabs;
        public static Dictionary<ProjectileType, GameObject> spraypaintPingPrefabs = new Dictionary<ProjectileType, GameObject>();

        public void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? filepath = null)
        {
#if DEBUG
            MelonLogger.Msg(Path.GetFileName(filepath) + ":" + caller + "." + lineNumber + ": " + message);
#endif
        }

        public override void OnInitializeMelon()
        {
            #if DEBUG
                LogMessage("Initializing Melon.");
            #endif

            ClassInjector.RegisterTypeInIl2Cpp<TweenManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PingManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PingComponent>();
            LoadEmbeddedAssetBundle();
            LoadEmbeddedAssetBundle2();

            MotionTracker.Settings.OnLoad();
        }

        public static void LoadEmbeddedAssetBundle()    // Orginal AssetBundle with original prefabs
        {
            MemoryStream memoryStream;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MotionTracker.Resources.motiontracker");
            memoryStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(memoryStream);

            assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());

        }
        public static void LoadEmbeddedAssetBundle2()   // Additional AssetBundle with additional prefabs (Cougar, Arrow, Coal, Raw Fish, Lost and Found Box)
        {
            MemoryStream memoryStream;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MotionTracker.Resources.motiontrackerassetbundleprefab");
            memoryStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(memoryStream);

            assetBundle2 = AssetBundle.LoadFromMemory(memoryStream.ToArray());

        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
#if DEBUG
            //LogMessage($"Scene {sceneName} with build index {buildIndex} has been loaded.");    // CLM
#endif
            if (sceneName.Contains("MainMenu"))
            {
                //SCRIPT_InterfaceManager/_GUI_Common/Camera/Anchor/Panel_OptionsMenu/Pages/ModSettings/GameObject/ScrollPanel/Offset/
#if DEBUG
                // LogMessage("Scene name containing MainMenu " + sceneName + " was loaded.");
#endif
                PingManager.inMenu = true;
                
                FirstTimeSetup();
            }
            else if (sceneName.Contains("SANDBOX") && motionTrackerParent)
            {

#if DEBUG
                    // LogMessage("Scene name containing SANDBOX " + sceneName + " was loaded.");
#endif

                if (PingManager.instance)
                {
                    PingManager.instance.ClearIcons();
                }
                PingManager.inMenu = false;
            }
            else
            {
#if DEBUG
                // LogMessage("Non-Menu and Non-Sandbox scene " + sceneName + " was loaded.");
#endif
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