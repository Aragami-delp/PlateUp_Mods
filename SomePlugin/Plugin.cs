using BepInEx;
using BepInEx.Logging;
using Kitchen;
using TMPro;
using HarmonyLib;
using Kitchen.Layouts;
using System.Collections.Generic;
using Kitchen.Layouts.Modules;
using System;
using HarmonyLib.Tools;

namespace SomePlugin
{
    [BepInPlugin("com.aragami.plateup.mods", "SomePlugin", "1.0.0")]
    [BepInProcess("PlateUp.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private readonly Harmony m_harmony = new Harmony("com.aragami.plateup.mods.harmony");

        private void Awake()
        {
            Log = base.Logger;
            m_harmony.PatchAll();
            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    //[HarmonyPatch(typeof(LayoutBlueprint), MethodType.Constructor, new Type[] { typeof(LayoutBlueprint) })]
    //public static class LayoutBlueprintPatch
    //{
    //    [HarmonyPrefix]
    //    // ReSharper disable once UnusedMember.Local
    //    static void StartPatch(ref LayoutBlueprint source)
    //    {
    //        if (source != null)
    //        {
    //            string output = String.Empty;
    //            foreach (KeyValuePair<LayoutPosition, Room> tile in source.Tiles)
    //            {
    //                output += tile.Key.x.ToString() + tile.Key.y.ToString() + tile.Value.Type.ToString();
    //            }
    //            Plugin.Log.LogInfo(output);
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(LayoutBlueprint), MethodType.Constructor, new Type[] { typeof(LayoutBlueprint) })]
    //public static class LayoutBlueprintConstructorPatch
    //{
    //    [HarmonyPrefix]
    //    // ReSharper disable once UnusedMember.Local
    //    static void StartPatch()
    //    {
    //        Plugin.Log.LogInfo("Is loaded");
    //    }
    //}

    [HarmonyPatch(typeof(LayoutBlueprint), nameof(LayoutBlueprint.Blank))]
    public static class LayoutBlueprintBlankPatch
    {
        [HarmonyPrefix]
        // ReSharper disable once UnusedMember.Local
        static void StartPatch(ref int width, ref int height, ref RoomType type)
        {
            Plugin.Log.LogInfo("Width: " + width.ToString() + ", Height: " + height + ", Type: " + type);
        }
    }

    [HarmonyPatch(typeof(RoomGrid), "Generate")]
    public static class RoomGridGeneratePatch
    {
        [HarmonyPrefix]
        // ReSharper disable once UnusedMember.Local
        static void StartPatch(ref int ___Width, ref int ___Height)
        {
            Plugin.Log.LogInfo("Width: " + ___Width.ToString() + ", Height: " + ___Height);
            ___Width = 7;
            ___Height = 5;
        }
    }

    #region DebugLog
    //[HarmonyPatch(typeof(RoomGrid), "ActOn")]
    //public static class RoomGridPatch
    //{
    //    [HarmonyPrefix]
    //    // ReSharper disable once UnusedMember.Local
    //    static void StartPatch(ref RoomGrid __instance)
    //    {
    //        Plugin.Log.LogInfo("Width: " + __instance.Width + "; Height: " + __instance.Height);
    //    }
    //}

    [HarmonyPatch(typeof(LayoutDesign), "Resize")]
    public static class LayoutDesignPatch2
    {
        [HarmonyPrefix]
        // ReSharper disable once UnusedMember.Local
        static void StartPatch(ref int w, ref int h)
        {
            Plugin.Log.LogInfo("W: " + w + "; H: " + h);
        }
    }
    #endregion

    #region TextInjection
    [HarmonyPatch(typeof(NewspaperSubview), "SetLossReason")]
    public static class NewspaperSubviewPatch
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        static void StartPatch(ref NewspaperSubview __instance, LossReason reason)
        {
            __instance.Tagline.text += " -- Aragami was here";
        }
    }

    [HarmonyPatch(typeof(DisplayVersion), "Awake")]
    public static class DisplayVersionPatch
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        static void StartPatch(ref DisplayVersion __instance)
        {
            __instance.Text.text = "Aragami was here\n" + __instance.Text.text;
        }
    }
    #endregion
}
