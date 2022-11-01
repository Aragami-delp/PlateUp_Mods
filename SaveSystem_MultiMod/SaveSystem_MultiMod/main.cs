#if MelonLoader
using MelonLoader;
#endif
#if BepInEx
using BepInEx;
using BepInEx.Logging;
#endif

using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using Kitchen;
using Kitchen.Modules;
using SaveSystem;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SaveSystem_MultiMod
{
#if MelonLoader
    // TODO: Proper Melon Setup
    public class SaveSystem_ModLoaderSystem : MelonMod
    {
        private static MelonLogger.Instance Log;
        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            LogInfo($"Plugin SaveSystem is loaded!");

            GameObject saveSystemMod = new GameObject("SaveSystem");
            saveSystemMod.AddComponent<SaveSystemMod>();
            UnityEngine.Object.DontDestroyOnLoad(saveSystemMod);
        }

        public static void LogInfo(string _log) { Log.Msg(_log); }
        public static void LogWarning(string _log) { Log.Warning(_log); }
        public static void LogError(string _log) { Log.Error(_log); }
    }
#endif

#if BepInEx
    [BepInPlugin("com.aragami.plateup.mods", "SaveSystem", "1.1.0")]
    [BepInProcess("PlateUp.exe")]
    public class SaveSystem_ModLoaderSystem : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = base.Logger;
            LogInfo($"Plugin SaveSystem is loaded!");

            GameObject saveSystemMod = new GameObject("SaveSystem");
            saveSystemMod.AddComponent<SaveSystemMod>();
            DontDestroyOnLoad(saveSystemMod);
        }

        public static void LogInfo(string _log) { Log.LogInfo(_log); }
        public static void LogWarning(string _log) { Log.LogWarning(_log); }
        public static void LogError(string _log) { Log.LogError(_log); }
    }
#endif

    public class SaveSystemMod : MonoBehaviour
    {
        /// <summary>
        /// Select menu options
        /// </summary>
        public static Option<string> SaveSystemOption;
        public static DisplayVersion ShowVersionSave;
        public static string ShowVersionSaveDefaultText;
        /// <summary>
        /// Button to save the current run
        /// </summary>
        public static ButtonElement SaveButton = null;
        /// <summary>
        /// Button to load the selected save file
        /// </summary>
        public static ButtonElement LoadButton = null;

        public static OptionsMenu<PauseMenuAction> CurrentMenu = null;
        public static int CurrentPlayerID = 0;
        /// <summary>
        /// Confirmation trigger to avoid overriding the current run upon loading a new one
        /// </summary>
        public static bool TryLoadedOnce = false;

        private readonly HarmonyLib.Harmony m_harmony = new HarmonyLib.Harmony("com.aragami.plateup.mods.harmony");

        public static Dictionary<string, BepInEx.PluginInfo> LoadedPlugins => BepInEx.Bootstrap.Chainloader.PluginInfos;

        private void Awake()
        {
            m_harmony.PatchAll();
        }

        public static void SaveRun(TextInputView.TextInputState _result, string _name)
        {
            if (_result != TextInputView.TextInputState.TextEntryComplete)
                return;
            _name = (String.IsNullOrWhiteSpace(_name) ? BackupSystem.GetCurrentRunUnixName() : _name);
            SaveSystem_ModLoaderSystem.LogInfo("Saving current run: " + _name);
            BackupSystem.SaveCurrentRun(_name);
            CurrentMenu.ModuleList.Clear();
            CurrentMenu.Setup(CurrentPlayerID);
            CurrentMenu.ModuleList.Select(SaveSystemMod.SaveButton);
        }
    }
    #region Reflection GetMethod
    public static class Helper
    {
        /// <summary>
        /// Gets a MethodInfo of a given class using Reflection, that doesn't have parameters
        /// </summary>
        /// <param name="_typeOfOriginal">Type of class to find a Method on</param>
        /// <param name="_name">Name of the Method to find</param>
        /// <param name="_genericT">Type of Method</param>
        /// <returns>MethodInfo if found</returns>
        public static MethodInfo GetMethod(Type _typeOfOriginal, string _name, Type _genericT = null)
        {
            MethodInfo retVal = _typeOfOriginal.GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (_genericT != null)
            {
                retVal = retVal.MakeGenericMethod(_genericT);
            }
            return retVal;
        }

        /// <summary>
        /// Gets a MethodInfo of a given class using Reflection, that has Parameters
        /// </summary>
        /// <param name="_typeOfOriginal">Type of class to find a Method on</param>
        /// <param name="_name">Name of the Method to find</param>
        /// <param name="_paramTypes">Types of parameters of the Method in right order</param>
        /// <param name="_genericT">Type of Method</param>
        /// <returns>MethodInfo if found</returns>
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
    #endregion

    #region Add options in menu
    [HarmonyPatch(typeof(OptionsMenu<PauseMenuAction>), nameof(OptionsMenu<PauseMenuAction>.Setup))]
    public static class OptionsMenuSetupPatch
    {
        //[HarmonyPostfix]
        // ReSharper disable once UnusedMember.Local
        static void Postfix(OptionsMenu<PauseMenuAction> __instance, int player_id)
        {
            if (Session.CurrentGameNetworkMode != GameNetworkMode.Host || GameInfo.CurrentScene != SceneType.Franchise)
                return;
            MethodInfo m_newSpacer = Helper.GetMethod(typeof(OptionsMenu<PauseMenuAction>), "New", typeof(SpacerElement));
            MethodInfo m_addLabelMethod = Helper.GetMethod(typeof(OptionsMenu<PauseMenuAction>), "AddLabel");
            MethodInfo m_addSelectMethod = Helper.GetMethod(typeof(OptionsMenu<PauseMenuAction>), "AddSelect", new Type[] { typeof(List<string>), typeof(Action<int>), typeof(int) });
            MethodInfo m_addButton = Helper.GetMethod(typeof(OptionsMenu<PauseMenuAction>), "AddButton", new Type[] { typeof(string), typeof(Action<int>), typeof(int), typeof(float), typeof(float) });
            m_newSpacer.Invoke(__instance, new object[1] { true }); // Default parameter bool = true
            m_addLabelMethod.Invoke(__instance, new string[] { "Save System" });

            BackupSystem.ReloadSaveSystem();
            if (BackupSystem.SaveFileNames.Count > 0)
            {
                #region Load
                // Select
                List<string> unixNames = new List<string>();
                List<string> displayNames = new List<string>();
                foreach (KeyValuePair<string, string> saveNameEntry in BackupSystem.SaveFileNames)
                {
                    unixNames.Add(saveNameEntry.Key);
                    string value = saveNameEntry.Value;
                    displayNames.Add(BackupSystem.IsUnixTimestamp(value) ? BackupSystem.UnixTimeToLocalDateTimeFormat(value) : value);
                }

                SaveSystemMod.SaveSystemOption = new Option<string>(unixNames, BackupSystem.CurrentSaveExists ? BackupSystem.GetCurrentRunUnixName() : unixNames[0], displayNames);
                BackupSystem.SelectedSaveSlotUnixName = BackupSystem.CurrentSaveExists ? BackupSystem.GetCurrentRunUnixName() : unixNames[0];
                SaveSystemMod.SaveSystemOption.OnChanged += (EventHandler<string>)((_, selectedSaveSlotIndex) =>
                {
                    BackupSystem.SelectedSaveSlotUnixName = selectedSaveSlotIndex;
                    SaveSystemMod.TryLoadedOnce = false;
                    SaveSystemMod.LoadButton?.SetLabel(BackupSystem.CurrentSelectionLoaded ? "Selection already loaded" : "Press to load!");
                    //SaveButton?.SetLabel(BackupSystem.CurrentSaveExists ? "Run already saved" : "Press to save!"); // Do I need both to be set? - prob only load
                });
                /*Plugin.SaveSystemModule = (IModule) */ // Not sure yet, what this is used for
                m_addSelectMethod.Invoke(__instance, new object[] { SaveSystemMod.SaveSystemOption.Names, new Action<int>(SaveSystemMod.SaveSystemOption.SetChosen), SaveSystemMod.SaveSystemOption.Chosen }); // All 3 parameters (since it is inline with only one)
                SaveSystemMod.LoadButton = (ButtonElement)m_addButton.Invoke(__instance, new object[] { BackupSystem.CurrentSelectionLoaded ? "Selection already loaded" : "Press to load!", (Action<int>)(_ =>
                {
                    if (!BackupSystem.CurrentSelectionLoaded)
                    {
                        if (!SaveSystemMod.TryLoadedOnce && !BackupSystem.CurrentSaveExists && BackupSystem.CurrentlyAnyRunLoaded)
                        {
                            SaveSystemMod.TryLoadedOnce = true;
                            SaveSystemMod.LoadButton.SetLabel("Override current run?");
                        }
                        else
                        {
                            SaveSystem_ModLoaderSystem.LogInfo("Loading Save: " + BackupSystem.SelectedSaveSlotUnixName);
                            SaveSystemMod.TryLoadedOnce = false;
                            BackupSystem.LoadSaveSlot();
                            SaveSystemMod.ShowVersionSave.Text.text = SaveSystemMod.ShowVersionSaveDefaultText + "\n Selected Save:\n" + BackupSystem.SelectedSaveSlotDisplayName;
                            __instance.ModuleList.Clear();
                            __instance.Setup(player_id);
                            __instance.ModuleList.Select(SaveSystemMod.LoadButton);
                        }
                    }
                }), 0, 1f, 0.2f });
                #endregion
                #region Delete
                // Delete
                #endregion
            }

            #region Save
            // SaveButton
            if (BackupSystem.CurrentlyAnyRunLoaded)
            {
                SaveSystemMod.SaveButton = (ButtonElement)m_addButton.Invoke(__instance, new object[] { BackupSystem.CurrentSaveExists ? "Run already saved" : "Press to save!", (Action<int>)(_ =>
                {
                    if (!BackupSystem.CurrentSaveExists)
                    {
                        SaveSystemMod.CurrentMenu = __instance;
                        SaveSystemMod.CurrentPlayerID = player_id;
                        TextInputView.RequestTextInput("Enter save name:", /*TODO: Preset with franchise name*/"", 20 /*Maybe it won't fit within the TMP window, if it's longer*/, new Action<TextInputView.TextInputState, string>(SaveSystemMod.SaveRun));
                    }
                }), 0, 1f, 0.2f });
            }
            #endregion
        }
    }
    #endregion

    [HarmonyPatch(typeof(DisplayVersion), "Awake")]
    public static class DisplayVersionPatch
    {
        // ReSharper disable once UnusedMember.Local
        static void Postfix(ref DisplayVersion __instance)
        {
            SaveSystemMod.ShowVersionSave = __instance;
            SaveSystemMod.ShowVersionSaveDefaultText = __instance.Text.text;
            BackupSystem.ReloadSaveSystem();
            __instance.Text.text = SaveSystemMod.ShowVersionSaveDefaultText + "\n Selected Save:\n" + BackupSystem.SelectedSaveSlotDisplayName;
        }
    }
}
