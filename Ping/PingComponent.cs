using MelonLoader;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection; 
using System.Collections;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine.UI;
using Il2CppTLD.Logging;
using System.Runtime.CompilerServices;
using Il2CppNewtonsoft.Json;
using static Il2CppParadoxNotion.Services.Logger;

namespace MotionTracker
{
	public class PingComponent : MonoBehaviour
	{
        public PingComponent(IntPtr intPtr) : base(intPtr)
        {
        }
               
        public GameObject attachedGameObject;     
        public GearItem attachedGearItem;     
        public PingManager.AnimalType animalType;
        public ProjectileType spraypaintType;
        public PingCategory assignedCategory;
     
        public CanvasGroup canvasGroup;
        public GameObject iconObject;
        public bool isInitialized = false;
        public Image iconImage;
        float timer = 0f;           // Accumulate the time since last frame so we can do things after the trigger duration is elapsed (triggerTime).
        float triggerTime = 5f;     // Trigger duration.  When the acculated frame time exceeds this value, we do stuff and reset the timer to zero.

        public enum PingCategory
        {
          None, Animal, Spraypaint
        };

        public RectTransform rectTransform;
        public bool clampOnRadar = false;
        // public bool clampOnRadar = true;
        public static GameObject playerObject;

        public void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? filepath = null)
        {
#if DEBUG
            MelonLogger.Msg(Path.GetFileName(filepath) + ":" + caller + "." + lineNumber + ": " + message);
#endif
        }

        [HideFromIl2Cpp]
        public static bool IsRawFish(GearItem gi)
        {
            if (gi != null)
            {
                FoodItem fi = gi.GetComponent<FoodItem>();
                if (fi != null)
                {
                    if (fi.m_IsFish && fi.m_IsRawMeat)
                    {
                        return true;
                    }
                }
                //if (gi.name.Contains(""))
                //{
                //    return true;
                //}
            }
            return false;       // By default, return false.  Not a raw fish.
        }

        [HideFromIl2Cpp]
        public void CreateIcon()
        {
            if(assignedCategory == PingCategory.Animal)
            {
                iconObject = Instantiate(MotionTrackerMain.GetAnimalPrefab(animalType));    // Get the animal prefab from the MotionTrackerMain class.  This is a static method.
                iconImage = iconObject.GetComponent<Image>();   // Get the Image component from the iconObject.  This is the object that we will be moving around on the radar UI.
                iconImage.color = Settings.animalColor; //  Set the color of the icon to the animal color.  This is set in the settings menu.
            }
            else if (assignedCategory == PingCategory.Spraypaint)
            {
                iconObject = Instantiate(MotionTrackerMain.GetSpraypaintPrefab(spraypaintType));
                iconImage = iconObject.GetComponent<Image>();
                iconImage.color = Settings.spraypaintColor;
            }

            iconObject.transform.SetParent(PingManager.instance.iconContainer.transform, false);        // What is this doing?  Make this Icon a child of the iconContainer.  This is the radar UI object.
            iconObject.active = true;   //  Set the iconObject to active.  This is the object that we will be moving around on the radar UI.
            canvasGroup = iconObject.GetComponent<CanvasGroup>();   // Make sure we have a canvasGroup to control the visibility of the icon.
            rectTransform = iconObject.GetComponent<RectTransform>();   // Get the RectTransform of the iconObject.  This is the object that we will be moving around on the radar UI.
        }

        [HideFromIl2Cpp]
        public void DeleteIcon()
        {
            if (iconObject)
            {
#if DEBUG
                if (attachedGearItem)
                {
                    //LogMessage("pingComponent.name:attachedGearItem = (" + attachedGearItem.name + ":" + attachedGearItem.m_InstanceID + ")");
                }

                if (attachedGameObject)
                {
                    //LogMessage("pingComponent.name:attachedGameObject = (" + attachedGameObject.name + ":" + attachedGameObject.GetInstanceID() + ")");
                }

                if (!attachedGameObject && !attachedGearItem)
                {

                    //LogMessage("pingComponent.name = (" + this.name + ") attachedGearItem and attachedGameObject are both null!");
                }
#endif
                GameObject.Destroy(iconObject);
            }
            else
            {
#if DEBUG
                LogMessage("iconObject is null.  So not deleting iconObject for pingComponent.name = (" + this.name + ")");
#endif
            }
        }

            [HideFromIl2Cpp]
            public bool AllowedToShow()
            {
                if (assignedCategory == PingCategory.Animal)
                {
                    if (animalType == PingManager.AnimalType.Crow && Settings.options.showCrows)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Rabbit && Settings.options.showRabbits)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Stag && Settings.options.showStags)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Doe && Settings.options.showDoes)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Wolf && Settings.options.showWolves)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Timberwolf && Settings.options.showTimberwolves)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Bear && Settings.options.showBears)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Cougar && Settings.options.showCougars)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Moose && Settings.options.showMoose)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.PuffyBird && Settings.options.showPuffyBirds)
                    {
                        return true;
                    }

                    // Gear... Arrows, Coal, etc.
                    else if (animalType == PingManager.AnimalType.Arrow && Settings.options.showArrows)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.Coal && Settings.options.showCoal)
                    {
                        return true;
                    }
                    else if (animalType == PingManager.AnimalType.RawFish && Settings.options.showRawFish)
                    {
                    return true;
                    }
                    else if (animalType == PingManager.AnimalType.LostAndFoundBox && Settings.options.showLostAndFoundBox)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (assignedCategory == PingCategory.Spraypaint && Settings.options.showSpraypaint)
                {
                    return true;
                }

                return false;
            }

        [HideFromIl2Cpp]
        public static void ManualDelete(PingComponent pingComponent)
        {
            if (pingComponent != null)
            {
#if DEBUG
                if (pingComponent.attachedGearItem)
                {
                    //pingComponent.LogMessage("pingComponent.name:attachedGearItem = (" + pingComponent.name + ":" + pingComponent.attachedGearItem.m_InstanceID + ")");
                }
                
                if (pingComponent.attachedGameObject)
                {
                    //pingComponent.LogMessage("pingComponent.name:attachedGameObject = (" + pingComponent.name + ":" + pingComponent.attachedGameObject.GetInstanceID() + ")");
                }

                if (!pingComponent.attachedGameObject && !pingComponent.attachedGearItem)
                {
                    //pingComponent.LogMessage("pingComponent.name = (" + pingComponent.name + ") attachedGearItem and attachedGameObject are both null!");
                }
#endif

                pingComponent.DeleteIcon();
                GameObject.Destroy(pingComponent);
            }
            else
            {
#if DEBUG
                // pingComponent is null.  Can't use LogMessage because pingComponent is null.  So, use MelonLogger.Msg instead.    
                // MelonLogger.Msg("PingComponent.cs:ManualDelete.199 pingComponent is NULL so no delete.");
#endif
            }
        }

        [HideFromIl2Cpp]
        public void SetVisible(bool visibility)
        {

            if (!canvasGroup)   // canvasGroup is null.  So return.
            {
#if DEBUG
                // LogMessage("canvasGroup null so not setting visibity (" + visibility + ") for pingComponent.name = (" + this.name + ")");
                //LogMessage("canvasGroup null so not setting visibity (" + visibility + ") for pingComponent.name = (" + this.name + ":" + this.GetInstanceID() + ")");
#endif
                return; 
            }

            if (AllowedToShow() && visibility)  // Allowed to show and visibility is true
            {
#if DEBUG
                // LogMessage("Setting canvasGroup.alpha = 1f for pingComponent.name = (" + this.name + ":" + this.gameObject.GetInstanceID() + ")");
                //LogMessage("Setting canvasGroup.alpha = 1f for pingComponent.name = (" + this.name + ":" + this.GetInstanceID() + ")");
#endif
                try
                {
                    canvasGroup.alpha = 1f;
                }
                catch (Exception e)
                {
                    LogMessage("Exception thrown (" + e.Message + ") when setting canvasGroup.alpha = 1f for pingComponent.name = (" + this.name + ":" + this.GetInstanceID() + ")");
                    // throw;
                }

#if DEBUG
                if (animalType == PingManager.AnimalType.Coal)
                {
                   // LogMessage("Setting canvasGroup.alpha = 1f for pingComponent.name = (" + this.name + ":" + this.GetInstanceID() + ")");
                    // LogMessage("Setting canvasGroup.alpha = 1f for pingComponent.name = (" + this.name + ":" + this.gameObject.GetInstanceID() + ")");
                }
#endif 
            }
            else
            {   // Not allowed to show or visibility is false
#if DEBUG
                if (animalType == PingManager.AnimalType.Coal)
                {
                    // LogMessage("Setting canvasGroup.alpha = 0f for pingComponent.name = (" + this.name + ":" + this.gameObject.GetInstanceID() + ")");
                   // LogMessage("Setting canvasGroup.alpha = 0f for pingComponent.name = (" + this.name + ":" + this.GetInstanceID() + ")");
                }
#endif
                try
                    {
                    canvasGroup.alpha = 0f;
                }
                catch (Exception e)
                {
                    LogMessage("Exception thrown (" + e.Message + ") when setting canvasGroup.alpha = 0f for pingComponent.name = (" + this.name + ":" + this.GetInstanceID() + ")");
                    // LogMessage("Exception thrown (" + e.Message + ") when setting canvasGroup.alpha = 0f for pingComponent.name = (" + this.name + ")");
                    // throw;
                }
            }
        }

        [HideFromIl2Cpp]       
        public void Initialize(PingManager.AnimalType type)
        {
#if DEBUG
            // LogMessage("Initialize pingComponent.name = (" + this.name + ":" + this.gameObject.GetInstanceID() + ")");
#endif

            attachedGameObject = this.gameObject;
            animalType = type;
            assignedCategory = PingCategory.Animal;

            CreateIcon();

            isInitialized = true;
        }

        [HideFromIl2Cpp]
        public void Initialize(ProjectileType type)
        {
            attachedGameObject = this.gameObject;
            spraypaintType = type;
            assignedCategory = PingCategory.Spraypaint;
            

            CreateIcon();

            isInitialized = true;
        }

        [HideFromIl2Cpp]
        private void OnDisable()
        {
#if DEBUG
            // LogMessage("Deleting pingComponent for (" + this.animalType + ")");
#endif
            DeleteIcon();
        }

        public void Update()
        {
            if (Settings.options.enableMotionTracker && PingManager.isVisible)
            {
                if (SaveGameSystem.m_CurrentGameMode == SaveSlotType.SANDBOX)
                {
                    if (GameManager.GetVpFPSPlayer() != null)
                    {
                        timer += Time.deltaTime;    // Accumulated time since we last logged stuff

                        // Address stuff displaying on radar that aren't there.
                        BaseAi baseAi = gameObject.GetComponent<BaseAi>();
                        if (baseAi != null)
                        {
#if DEBUG
                            if (timer > triggerTime)
                            {
                                //LogMessage("(" + this.gameObject.name + ":" + this.gameObject.GetInstanceID() + ") baseAi.currentmode = (" + baseAi.m_CurrentMode + ")");
                            }
#endif
                            if (baseAi.m_CurrentMode == AiMode.Dead)
                            {
#if DEBUG
                                //LogMessage("Deleting pingComponent for (" + this.gameObject.name + ":" + this.gameObject.GetInstanceID() + ")");
#endif
                                ManualDelete(this);
                                return;
                            }
                        }

                        UpdateLocatableIcons();

                        // Check if we need to reset the accumulated time
                        if (timer > triggerTime)
                        {
                            // LogMessage("timer = (" + timer + ") which is greater than triggerTime = (" + triggerTime + ")");
                            // LogMessage("GameManager.GetVpFPSPlayer().gameObject.transform.position = (" + GameManager.GetVpFPSPlayer().gameObject.transform.position + ")");
                            timer = 0f;
                            // LogMessage("timer reset to 0 (" + timer + ") and triggerTime = (" + triggerTime + ")");
                        }
                    }
                }
            }
        }

        private void UpdateLocatableIcons()
        {
            if (TryGetIconLocation(out var iconLocation))
            {
                // LogMessage("See something to be updated. (" + this.name + ")");

                SetVisible(true);
                if (!rectTransform)   // rectTransform is null.  So delete the pingComponent and return.
                {
                    ManualDelete(this);

                    #if DEBUG
                        // LogMessage("rectTransform null so ignoring pingComponent.name = (" + this.name + ":" + GetInstanceID() + ")");
                    #endif
                    
                    return;
                }

                rectTransform.anchoredPosition = iconLocation;

                // LogMessage("anchoredPosition = " + rectTransform.anchoredPosition);

                if (assignedCategory == PingCategory.Spraypaint)
                {
                    // LogMessage("Assigned category is Spraypaint = " + assignedCategory);

                    if (iconImage.color != Settings.spraypaintColor || rectTransform.localScale != Settings.spraypaintScale)
                    {
                        rectTransform.localScale = Settings.spraypaintScale;
                        iconImage.color = Settings.spraypaintColor;
                    }
                }
                else if (assignedCategory == PingCategory.Animal)
                {
                    if (iconImage.color != Settings.animalColor || rectTransform.localScale != Settings.animalScale)    // Should this be using animalScale instead of sprayScale?
                    {
                        rectTransform.localScale = Settings.animalScale;
                        iconImage.color = Settings.animalColor;
                    }

                    if (this.name.Contains("Arrow"))
                    {
                        iconImage.color = Color.yellow;    // Color the arrows to help make them show up for easier viewing.
                    }

#if DEBUG
                    if ((timer > triggerTime) && (name.Contains("RawFish")))
                    {
                        //LogMessage("Radar RawFish updating (" + this.name + ":" + this.attachedGearItem.m_InstanceID + ") position is (" + this.transform.position + ")");
                    }

                    //if (timer > triggerTime)
                    //{
                    //    if (name.Contains("Arrow"))
                    //    {
                    //        LogMessage("Radar Arrow updating (" + this.name + ":" + this.attachedGearItem.m_InstanceID + ") position is (" + this.transform.position + ")");
                    //    }
                    //    else if (name.Contains("Coal"))
                    //        {
                    //            LogMessage("Radar Coal updating (" + this.name + ":" + this.attachedGearItem.m_InstanceID + ") position is (" + this.transform.position + ")");
                    //        }
                    //    else if (assignedCategory == PingCategory.Animal)   // Some other GearItem "Animal"
                    //    {
                    //        LogMessage("Radar Animal updating (" + this.name + ":" + this.attachedGearItem.m_InstanceID + ") position is (" + this.transform.position + ")");
                    //    }
                    //    else if (name.Contains("DecalContainer")) // SprayPaint Decal?  Should never be here.
                    //    {
                    //        // LogMessage("Radar DecalContainer updating (" + this.name + ":" + this.attachedGameObject.GetInstanceID() + ") position is (" + this.transform.position + ")");
                    //    }
                    //    else
                    //    {
                    //        LogMessage("Radar ??? updating (" + this.name + ":" + this.attachedGameObject.GetInstanceID() + ") position is (" + this.transform.position + ")");
                    //    }
                    //}
#endif
                }
            }
            else
            {
#if DEBUG
                if ((timer > triggerTime) && (name.Contains("RawFish")))
                {
                    //LogMessage("GearItem:ID (" + this.attachedGearItem.name + ":" + this.attachedGearItem.m_InstanceID + ") assigned category is Animal.RawFish and setting to NOT visible.");
                    // LogMessage("(" + name + ":" + GetInstanceID() + ") Setting visible to FALSE.");   // Lot of data!
                }
#endif
                SetVisible(false);
            }
        }

        private bool TryGetIconLocation(out Vector2 iconLocation)
        {
            iconLocation = GetDistanceToPlayer(this);

            float radarSize = GetRadarUISize();

            var scale = radarSize / Settings.options.detectionRange;

#if DEBUG
            if (name.Contains("RawFish"))
            {
                if (timer > triggerTime)
                {
                    //LogMessage("(" + this.name + ":" + this.attachedGearItem.GetInstanceID() + ") distance to player=" + iconLocation + " radarSize=" + radarSize + " scale=" + scale + " scaled iconLocation=" + iconLocation * scale);
                }
            }
#endif

            iconLocation *= scale;

            // Rotate the icon by the players y rotation if enabled
            if (PingManager.instance.applyRotation)
            {
                var playerForwardDirectionXZ = new Vector3(0, 0, 0);

                // Get the forward vector of the player projected on the xz plane
                if (GameManager.GetVpFPSPlayer())
                {
                    playerForwardDirectionXZ = Vector3.ProjectOnPlane(GameManager.GetVpFPSPlayer().gameObject.transform.forward, Vector3.up);
                }

                // Create a rotation from the direction
                var rotation = Quaternion.LookRotation(playerForwardDirectionXZ);

                // Mirror y rotation
                var euler = rotation.eulerAngles;
                euler.y = -euler.y;
                rotation.eulerAngles = euler;

                // Rotate the icon location in 3D space
                var rotatedIconLocation = rotation * new Vector3(iconLocation.x, 0.0f, iconLocation.y);

                // Convert from 3D to 2D
                iconLocation = new Vector2(rotatedIconLocation.x, rotatedIconLocation.z);
            }

#if DEBUG
            if (this.name.Contains("Coal"))
            {
                if (timer > triggerTime)
                {
                    //LogMessage("GameObject:ID (" + this.attachedGearItem.name + ":" + this.gameObject.GetInstanceID() + ") assigned category is Animal.Coal and final distance(iconLocation) = " + iconLocation);
                }
            }
#endif

            if (iconLocation.sqrMagnitude < radarSize * radarSize || this.clampOnRadar)
            {
                // Make sure it is not shown outside the radar
                iconLocation = Vector2.ClampMagnitude(iconLocation, radarSize);
#if DEBUG
                if (this.name.Contains("Coal"))
                {
                    if (timer > triggerTime)
                    {
                        //LogMessage("GameObject:ID (" + this.gameObject.name + ":" + this.attachedGearItem.GetInstanceID() + ") inside radar display. ClampMagnitude distance(iconLocation) = " + iconLocation);
                    }
                }
#endif

                return true;
            }
            else
            {
                // gameObject is outside radar reporting area.  Can we check if it has a icon that we can remove?
                // if you delete here for animals like the cougar, then the cougar will not be visible on the radar.
                // And, the cougar will NOT reappear on the radar when it comes back into the radar reporting area because we rely on the BaseAi-"Start" event to detect the existence of the cougar.
                // So, still have an issue with orphaned cougar icons on the radar.
                // Ok... I don't think we want to delete anything at this point.  Just return false.  The calling code will hide the icon if we retrun false.

                //if (this.name.Contains("Arrow"))    // Only delete if it's an arrow-ish thing...
                //    {
                //        ManualDelete(this);
                //}

#if DEBUG
                if (this.name.Contains("Coal"))
                //if (assignedCategory == PingCategory.Animal)    // Leave the spraypaint decals alone...
                {
                    if (timer > triggerTime)
                    {
                        //LogMessage("GameObject:ID (" + this.gameObject.name + ":" + this.gameObject.GetInstanceID() + ") detected outside radar display. sqrMagnitude distance(iconLocation.sqrMagnitude) = " + iconLocation.sqrMagnitude);
                    }
                }
#endif

                return false;
            }
        }

        private float GetRadarUISize()
        {
            return PingManager.instance.iconContainer.rect.width / 2;
        }

        private Vector2 GetDistanceToPlayer(PingComponent locatable)
        {
            if (GameManager.GetVpFPSPlayer() && locatable)
            {
                Vector3 distanceToPlayer = locatable.transform.position - GameManager.GetVpFPSPlayer().gameObject.transform.position;

#if DEBUG
                if (timer > triggerTime)
                {

                    if (locatable.name.Contains("Arrow") || locatable.name.Contains("Coal"))
                    {
                        //LogMessage("Gear item (" + this.name + ":" + this.attachedGearItem.m_InstanceID + ") position is (" + this.transform.position + ") and distance is " + distanceToPlayer);
                    }
                    else if (assignedCategory == PingCategory.Animal)
                    {
                        //LogMessage("Animal (" + this.name + ":" + this.attachedGameObject.GetInstanceID() + ") position is (" + this.transform.position + ") and distance is " + distanceToPlayer);
                    }
                    else if (locatable.name.Contains("DecalContainer")) // SprayPaint Decal
                    {
                        // LogMessage("DecalContainer (" + this.name + ":" + this.attachedGameObject.GetInstanceID() + ") position is (" + this.transform.position + ") and distance is " + distanceToPlayer);
                    }
                    else
                    {
                        //LogMessage("??? (" + this.name + ":" + this.attachedGameObject.GetInstanceID() + ") position is (" + this.transform.position + ") and distance is " + distanceToPlayer);
                    }
                }
#endif
                return new Vector2(distanceToPlayer.x, distanceToPlayer.z);
            }

            return new Vector2(0, 0);
        }
    }
}