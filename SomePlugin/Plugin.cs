using BepInEx;
using BepInEx.Logging;
using Kitchen;
using Kitchen.Modules;
using KitchenData;
using UnityEngine;
using TMPro;
using HarmonyLib;
using Kitchen.Layouts;
using System.Collections.Generic;
using Kitchen.Layouts.Modules;
using System;
using System.Reflection;
using HarmonyLib.Tools;
using System.Linq;
using System.Reflection.Emit;

namespace SomePlugin
{
    [BepInPlugin("com.aragami.plateup.mods", "SaveSystem", "0.1.0")]
    [BepInProcess("PlateUp.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        public static Option<float> SaveSystemOption;
        public static IModule SaveSystemModule;

        private readonly Harmony m_harmony = new Harmony("com.aragami.plateup.mods.harmony");

        private void Awake()
        {
            Log = base.Logger;
            m_harmony.PatchAll();
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    public static class Helper
    {
        public static MethodInfo GetMethod(Type _typeOfOriginal, string _name, Type _genericT = null)
        {
            MethodInfo retVal = _typeOfOriginal.GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (_genericT != null)
            {
                retVal = retVal.MakeGenericMethod(_genericT);
            }
            return retVal;
        }

        public static MethodInfo GetMethod(Type _typeOfOriginal, string _name, Type[] _paramTypes, Type _genericT = null)
        {
            MethodInfo retVal = _typeOfOriginal.GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Instance, null, _paramTypes, null);
            if (_genericT != null)
            {
                retVal = retVal.MakeGenericMethod(_genericT);
            }
            return retVal;
        }
    }

    #region Add options in menu
    [HarmonyPatch(typeof(OptionsMenu<PauseMenuAction>), nameof(OptionsMenu<PauseMenuAction>.Setup))]
    public static class OptionsMenuSetupPatch
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        static void StartPatch(OptionsMenu<PauseMenuAction> __instance)
        {
            MethodInfo m_newMethod = Helper.GetMethod(typeof(OptionsMenu<PauseMenuAction>), "New", typeof(SpacerElement));
            MethodInfo m_addLabelMethod = Helper.GetMethod(typeof(OptionsMenu<PauseMenuAction>), "AddLabel");
            MethodInfo m_addSelectMethod = Helper.GetMethod(typeof(OptionsMenu<PauseMenuAction>), "AddSelect", new Type[] { typeof(List<string>), typeof(Action<int>), typeof(int) });
            m_newMethod.Invoke(__instance, new object[1] { true }); // Default parameter bool = true
            m_addLabelMethod.Invoke(__instance, new string[] { "Save System" });

            // Select
            Plugin.SaveSystemOption = new Option<float>(new List<float> { 0f, 1f, 2f }, 0, new List<string> { "Save0", "Save1", "Save2" });
            Plugin.SaveSystemOption.OnChanged += (EventHandler<float>)((_, f) =>
            {
                Plugin.Log.LogInfo(f.ToString());
            });
            /*Plugin.SaveSystemModule = (IModule) */
            m_addSelectMethod.Invoke(__instance, new object[] { Plugin.SaveSystemOption.Names, new Action<int>(Plugin.SaveSystemOption.SetChosen), Plugin.SaveSystemOption.Chosen }); // All 3 parameters (since it is inline with only one)

        }
    }
    #endregion

    #region Change map sizes
    //[HarmonyPatch(typeof(RoomGrid), "Generate")]
    //public static class RoomGridGeneratePatch
    //{
    //    [HarmonyPrefix]
    //    // ReSharper disable once UnusedMember.Local
    //    static void StartPatch(ref int ___Width, ref int ___Height)
    //    {
    //        Plugin.Log.LogInfo("Width: " + ___Width.ToString() + ", Height: " + ___Height);
    //        ___Width = 7;
    //        ___Height = 5;
    //    }
    //}
    #endregion

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

    //[HarmonyPatch(typeof(LayoutDesign), "Resize")]
    //public static class LayoutDesignPatch2
    //{
    //    [HarmonyPrefix]
    //    // ReSharper disable once UnusedMember.Local
    //    static void StartPatch(ref int w, ref int h)
    //    {
    //        Plugin.Log.LogInfo("W: " + w + "; H: " + h);
    //    }
    //}
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
