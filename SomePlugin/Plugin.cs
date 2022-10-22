using BepInEx;
using BepInEx.Logging;
using Kitchen;
using Kitchen.Modules;
using HarmonyLib;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace SaveSystem
{
    [BepInPlugin("com.aragami.plateup.mods", "SaveSystem", "1.0.1")]
    [BepInProcess("PlateUp.exe")]
    public class SaveSystemPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        /// <summary>
        /// Select menu options
        /// </summary>
        public static Option<string> SaveSystemOption;

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

        private readonly Harmony m_harmony = new Harmony("com.aragami.plateup.mods.harmony");

        private void Awake()
        {
            Log = base.Logger;
            m_harmony.PatchAll();
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static void SaveRun(TextInputView.TextInputState _result, string _name)
        {
            if (_result != TextInputView.TextInputState.TextEntryComplete)
                return;
            _name = (String.IsNullOrWhiteSpace(_name) ? BackupSystem.GetCurrentRunUnixName() : _name);
            SaveSystemPlugin.Log.LogInfo("Saving current run: " + _name);
            BackupSystem.SaveCurrentRun(_name);
            CurrentMenu.ModuleList.Clear();
            CurrentMenu.Setup(CurrentPlayerID);
            CurrentMenu.ModuleList.Select(SaveSystemPlugin.SaveButton);
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

            // Select
            BackupSystem.ReloadSaveSystem();
            if (BackupSystem.SaveFileNames.Count > 0)
            {
                List<string> unixNames = new List<string>();
                List<string> displayNames = new List<string>();
                foreach (KeyValuePair<string, string> saveNameEntry in BackupSystem.SaveFileNames)
                {
                    unixNames.Add(saveNameEntry.Key);
                    string value = saveNameEntry.Value;
                    displayNames.Add(BackupSystem.IsUnixTimestamp(value) ? BackupSystem.UnixTimeToLocalDateTimeFormat(value) : value);
                }

                SaveSystemPlugin.SaveSystemOption = new Option<string>(unixNames, BackupSystem.CurrentSaveExists ? BackupSystem.GetCurrentRunUnixName() : unixNames[0], displayNames);
                BackupSystem.SelectedSaveSlotUnixName = BackupSystem.CurrentSaveExists ? BackupSystem.GetCurrentRunUnixName() : unixNames[0];
                SaveSystemPlugin.SaveSystemOption.OnChanged += (EventHandler<string>)((_, selectedSaveSlotIndex) =>
                {
                    BackupSystem.SelectedSaveSlotUnixName = selectedSaveSlotIndex;
                    SaveSystemPlugin.TryLoadedOnce = false;
                    SaveSystemPlugin.LoadButton?.SetLabel(BackupSystem.CurrentSelectionLoaded ? "Selection already loaded" : "Press to load!");
                    //SaveButton?.SetLabel(BackupSystem.CurrentSaveExists ? "Run already saved" : "Press to save!"); // Do I need both to be set? - prob only load
                });
                /*Plugin.SaveSystemModule = (IModule) */ // Not sure yet, what this is used for
                m_addSelectMethod.Invoke(__instance, new object[] { SaveSystemPlugin.SaveSystemOption.Names, new Action<int>(SaveSystemPlugin.SaveSystemOption.SetChosen), SaveSystemPlugin.SaveSystemOption.Chosen }); // All 3 parameters (since it is inline with only one)
                SaveSystemPlugin.LoadButton = (ButtonElement)m_addButton.Invoke(__instance, new object[] { BackupSystem.CurrentSelectionLoaded ? "Selection already loaded" : "Press to load!", (Action<int>)(_ =>
                {
                    if (!BackupSystem.CurrentSelectionLoaded)
                    {
                        if (!SaveSystemPlugin.TryLoadedOnce && !BackupSystem.CurrentSaveExists && BackupSystem.CurrentlyAnyRunLoaded)
                        {
                            SaveSystemPlugin.TryLoadedOnce = true;
                            SaveSystemPlugin.LoadButton.SetLabel("Override current run?");
                        }
                        else
                        {
                            SaveSystemPlugin.Log.LogInfo("Loading Save: " + BackupSystem.SelectedSaveSlotUnixName);
                            SaveSystemPlugin.TryLoadedOnce = false;
                            BackupSystem.LoadSaveSlot();
                            __instance.ModuleList.Clear();
                            __instance.Setup(player_id);
                            __instance.ModuleList.Select(SaveSystemPlugin.LoadButton);
                        }
                    }
                }), 0, 1f, 0.2f });
            }

            // SaveButton
            if (BackupSystem.CurrentlyAnyRunLoaded)
            {
                SaveSystemPlugin.SaveButton = (ButtonElement)m_addButton.Invoke(__instance, new object[] { BackupSystem.CurrentSaveExists ? "Run already saved" : "Press to save!", (Action<int>)(_ =>
                {
                    if (!BackupSystem.CurrentSaveExists)
                    {
                        SaveSystemPlugin.CurrentMenu = __instance;
                        SaveSystemPlugin.CurrentPlayerID = player_id;
                        TextInputView.RequestTextInput("Enter save name:", /*TODO: Preset with franchise name*/"", 20 /*Maybe it won't fit within the TMP window, if it's longer*/, new Action<TextInputView.TextInputState, string>(SaveSystemPlugin.SaveRun));
                    }
                }), 0, 1f, 0.2f });
            }
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
    //[HarmonyPatch(typeof(NewspaperSubview), "SetLossReason")]
    //public static class NewspaperSubviewPatch
    //{
    //    [HarmonyPostfix]
    //    // ReSharper disable once UnusedMember.Local
    //    static void StartPatch(ref NewspaperSubview __instance, LossReason reason)
    //    {
    //        __instance.Tagline.text += " -- Aragami was here";
    //    }
    //}

    [HarmonyPatch(typeof(DisplayVersion), "Awake")]
    public static class DisplayVersionPatch
    {
        // ReSharper disable once UnusedMember.Local
        static void Postfix(ref DisplayVersion __instance)
        {
            __instance.Text.text = "Mod loaded: SaveSystem\n" + __instance.Text.text;
        }
    }
    #endregion
}
