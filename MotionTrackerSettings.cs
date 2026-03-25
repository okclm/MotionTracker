using UnityEngine;
using ModSettings;
using MelonLoader;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace MotionTracker
{
    internal class MotionTrackerSettings : JsonModSettings
    {
        //[Section("General - Version: 1.2.0")]
        //[Section("General - Version: " + Assembly.GetExecutingAssembly().GetName().Version)]
        [Section("General")]

        [Name("Enable Motion Tracker")]
        [Description("Enable/Disable Motion Tracker")]
        public bool enableMotionTracker = true;

        [Name("Visibility")]
        [Description("Always visible / Visible on key toggle")]
        public Settings.DisplayStyle displayStyle = Settings.DisplayStyle.AlwaysOn;

        [Name("Toggle Key")]
        [Description("Toggle visibility on keypress")]
        public KeyCode toggleKey = KeyCode.Keypad0;

        [Name("Only outdoors")]
        [Description("Only enables overlay while outdoors")]
        public bool onlyOutdoors = true;

        [Name("Detection Range")]
        [Description("Range to detect motion")]
        [Slider(0, 800)]
        public int detectionRange = 100;

        [Name("Scale")]
        [Description("Scale of motion detector overlay")]
        [Slider(0, 4)]
        public float scale = 1f;

        [Name("Background Opacity")]
        [Description("Opacity of motion detector overlay")]
        [Slider(0, 1)]
        public float opacity = 0.7f;

        [Section("Spraypaint")]

        [Name("Show Spraypaint Markers")]
        [Description("Enable / Disable")]
        public bool showSpraypaint = true;

        [Name("Spraypaint Icon Scale")]
        [Description("Scale of spraypaint icons")]
        [Slider(0.2f, 5)]
        public float spraypaintScale = 2.0f;

        [Name("Spraypaint Opacity")]
        [Description("Opacity of spraypaint icons")]
        [Slider(0, 1)]
        public float spraypaintOpacity = 0.8f;

        [Section("Wildlife")]

        [Name("Animal Icon Scale")]
        [Description("Scale of animal icons")]
        [Slider(0, 5)]
        public float animalScale = 3.5f;

        [Name("Animal Icon Opacity")]
        [Description("Opacity of animal icons")]
        [Slider(0, 1)]
        public float animalOpacity = 0.8f;

        [Name("Show Crows")]
        [Description("Track motion of crows")]
        public bool showCrows = true;

        [Name("Show Rabbits")]
        [Description("Track motion of rabbits")]
        public bool showRabbits = true;

        [Name("Show Stags")]
        [Description("Track motion of stags")]
        public bool showStags = true;

        [Name("Show Does")]
        [Description("Track motion of does")]
        public bool showDoes = true;

        [Name("Show Wolves")]
        [Description("Track motion of wolves")]
        public bool showWolves = true;

        [Name("Show Timberwolves")]
        [Description("Track motion of timberwolves")]
        public bool showTimberwolves = true;

        [Name("Show Bears")]
        [Description("Track motion of bears")]
        public bool showBears = true;

        [Name("Show Cougars")]
        [Description("Track motion of cougars")]
        public bool showCougars = true;

        [Name("Show Moose")]
        [Description("Track motion of moose")]
        public bool showMoose = true;

        [Name("Show Puffy Birds")]
        [Description("Track motion of puffy birds")]
        public bool showPuffyBirds = true;

        [Section("Gear")]

        // TODO: Add gear icon scale and opacity

        [Name("Gear Icon Scale")]
        [Description("Scale of gear icons")]
        [Slider(0, 5)]
        public float gearScale = 3.5f;

        [Name("Gear Icon Opacity")]
        [Description("Opacity of gear icons")]
        [Slider(0, 1)]
        public float gearOpacity = 0.8f;

        [Name("Show Arrows")]
        [Description("Show Arrows on radar")]
        public bool showArrows = true;

        [Name("Show Coal")]
        [Description("Show Coal on radar")]
        public bool showCoal = true;

        [Name("Show Raw Fish")]
        [Description("Show Raw Fish on radar")]
        public bool showRawFish = true;

        [Name("Show Lost and Found Box")]
        [Description("Show Lost and Found Box on radar")]
        public bool showLostAndFoundBox = true;

        [Name("Show Salt Deposits")]
        [Description("Show Salt Deposits on radar")]
        public bool showSaltDeposit = true;

        [Name("Show Beach Loot")]
        [Description("Show Beach combing loot on radar")]
        public bool showBeachLoot = true;

        // TODO: Add debug logging level

        //[Section("Miscellaneous")]

        //[Name("Debug logging level")]
        //[Description("Set the troubleshooting debug logging level.\n0 = No logging, 1 = a little verbose, 2 = more verbose,\n3 = A Lot Verbose, 4 = A Lot More Verbose,\n5 = Kitchen Sink Verbose!")]
        //public Settings.DebugLoggingLevel logLevel = Settings.DebugLoggingLevel.No_Logging;

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
        }

        protected override void OnConfirm()
        {
            base.OnConfirm();

            if (PingManager.instance)
            {  
                PingManager.instance.SetOpacity(Settings.options.opacity);
                PingManager.instance.Scale(Settings.options.scale);

                // TODO: Add gear icon scale and opacity

                Settings.animalScale = new Vector3(Settings.options.animalScale, Settings.options.animalScale, Settings.options.animalScale);
                Settings.spraypaintScale = new Vector3(Settings.options.spraypaintScale, Settings.options.spraypaintScale, Settings.options.spraypaintScale);
                Settings.gearScale = new Vector3(Settings.options.gearScale, Settings.options.gearScale, Settings.options.gearScale);

                Settings.animalColor = new Color(1, 1, 1, Settings.options.animalOpacity);
                Settings.gearColor = new Color(1, 1, 1, Settings.options.gearOpacity);
                Settings.spraypaintColor = new Color(0.62f, 0.29f, 0.0f, Settings.options.spraypaintOpacity);
            }
        }
    }

    internal static class Settings
    {
        public static MotionTrackerSettings options;

        // TODO: Add gear icon scale and opacity

        public static Vector3 animalScale;
        public static Vector3 spraypaintScale;
        public static Vector3 gearScale;

        public static Color animalColor;
        public static Color spraypaintColor;
        public static Color gearColor;

        public static bool toggleBool = false;

        public enum DisplayStyle
        {
            AlwaysOn, Toggle
        };

        //public enum DebugLoggingLevel
        //{
        //    No_Logging, A_Little_Verbose, More_Verbose, A_Lot_Verbose, A_Lot_More_Verbose, Kitchen_Sink_Verbose
        //};

        public static void OnLoad()
        {
            options = new MotionTrackerSettings();
            options.AddToModSettings("Motion Tracker");

            // TODO: Add gear icon scale and opacity

            animalScale = new Vector3(options.animalScale, options.animalScale, options.animalScale);
            gearScale = new Vector3(options.gearScale, options.gearScale, options.gearScale);
            spraypaintScale = new Vector3(options.spraypaintScale, options.spraypaintScale, options.spraypaintScale);

            animalColor = new Color(1, 1, 1, options.animalOpacity);
            gearColor = new Color(1, 1, 1, options.gearOpacity);
            spraypaintColor = new Color(0.62f, 0.29f, 0.0f, options.spraypaintOpacity);
        }
    }
}
