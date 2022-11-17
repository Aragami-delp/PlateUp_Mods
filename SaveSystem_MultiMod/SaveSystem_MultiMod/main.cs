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
using TMPro;
using Unity.Entities;
#if MelonLoader
[assembly: MelonInfo(typeof(SaveSystem_MultiMod.SaveSystem_ModLoaderSystem), "SaveSystem", "1.3.0", "Aragami"), HarmonyDontPatchAll]
#endif
namespace SaveSystem_MultiMod
{
#if MelonLoader
    public class SaveSystem_ModLoaderSystem : MelonMod
    {
        private static MelonLogger.Instance Log;
        public override void OnInitializeMelon()
        {
            Debug.LogWarning("Mod: SaveSystemMod in use!"); // For log file output for official support staff

            Log = LoggerInstance;
            LogInfo("Plugin SaveSystem is loaded!");

            GameObject saveSystemMod = new GameObject("SaveSystem");
            saveSystemMod.AddComponent<SaveSystemMod>();
            UnityEngine.Object.DontDestroyOnLoad(saveSystemMod);
        }

        public static void LogInfo(string _log) { Log.Msg("SaveSystem: " + _log); }
        public static void LogWarning(string _log) { Log.Warning("SaveSystem: " + _log); }
        public static void LogError(string _log) { Log.Error("SaveSystem: " + _log); }
    }
#endif

#if BepInEx
    [BepInPlugin("com.aragami.plateup.mods.savesystem", "SaveSystem", "1.3.0")]
    [BepInProcess("PlateUp.exe")]
    public class SaveSystem_ModLoaderSystem : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Debug.LogWarning("Mod: SaveSystemMod in use!"); // For log file output for official support staff

            Log = base.Logger;
            LogInfo("Plugin SaveSystem is loaded!");

            GameObject saveSystemMod = new GameObject("SaveSystem");
            saveSystemMod.AddComponent<SaveSystemMod>();
            DontDestroyOnLoad(saveSystemMod);
        }

        public static void LogInfo(string _log) { Log.LogInfo("SaveSystem: " + _log); }
        public static void LogWarning(string _log) { Log.LogWarning("SaveSystem: " + _log); }
        public static void LogError(string _log) { Log.LogError("SaveSystem: " + _log); }
    }
#endif

    public class SaveSystemMod : MonoBehaviour
    {
        private readonly HarmonyLib.Harmony m_harmony = new HarmonyLib.Harmony("com.aragami.plateup.mods.harmony");

        private void Awake()
        {
            m_harmony.PatchAll();
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

        public static void ChangeScene(SceneType _next)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity entity = entityManager.CreateEntity((ComponentType)typeof(SPerformSceneTransition), (ComponentType)typeof(CDoNotPersist));
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.AddComponentData<SPerformSceneTransition>(entity, new SPerformSceneTransition()
            {
                NextScene = _next
            });
        }
    }
    #endregion

    #region Add SaveSystem to pause menu
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Setup))]
    public static class MainMenuSetupPatch
    {
        static void Prefix(MainMenu __instance, int player_id)
        {
            if (Session.CurrentGameNetworkMode != GameNetworkMode.Host || (GameInfo.CurrentScene != SceneType.Franchise && GameInfo.CurrentScene != SceneType.Kitchen))
                return;
            MethodInfo m_addButtonMenu = Helper.GetMethod(__instance.GetType(), "AddSubmenuButton");
            MethodInfo m_addBackToLobbyButton = Helper.GetMethod(__instance.GetType(), "AddButton");

            m_addButtonMenu.Invoke(__instance, new object[3] { "Save System", typeof(SaveSystemMenu), false });
            if (GameInfo.CurrentScene == SceneType.Kitchen)
                m_addBackToLobbyButton.Invoke(__instance, new object[5] { "Back to lobby", (Action<int>)(_ => Helper.ChangeScene(SceneType.Franchise)), 0, 1f, 0.2f });
        }
    }

    [HarmonyPatch(typeof(PlayerPauseView), "SetupMenus")]
    class PlayerPauseView_Patch
    {
        [HarmonyPrefix]
        static void Prefix(PlayerPauseView __instance)
        {
            ModuleList moduleList = (ModuleList)__instance.GetType().GetField("ModuleList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            MethodInfo mInfo = Helper.GetMethod(__instance.GetType(), "AddMenu");

            mInfo.Invoke(__instance, new object[2] { typeof(SaveSystemMenu), new SaveSystemMenu(__instance.ButtonContainer, moduleList) });
        }
    }
    #endregion

    #region ReworkUI
    public class SaveSystemDeleteMenu : Menu<PauseMenuAction>
    {
        public SaveSystemDeleteMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        public override void Setup(int player_id)
        {
            AddLabel("Delete this save?");
            AddInfo(SaveSystemMenu.currentlySelectedName);
            AddInfo(SaveSystemMenu.currentlySelectedDateTime);
            this.AddButton(this.Localisation["PROFILE_CONFIRM_DELETE"], (Action<int>)(i => this.ConfirmDelete()));
            this.AddButton(this.Localisation["CANCEL_PROFILE"], (Action<int>)(i => this.RequestPreviousMenu()));
        }

        public void ConfirmDelete()
        {
            SaveSystem_ModLoaderSystem.LogInfo("Deleting run: " + SaveSystemMenu.currentlySelectedName);
            SaveSystemManager.Instance.DeleteSave(SaveSystemMenu.currentlySelectedName);
            SaveSystemMenu.currentlySelectedName = null;
            this.RequestPreviousMenu();
        }
    }

    public class SaveSystemLoadConfirmMenu : Menu<PauseMenuAction>
    {
        public SaveSystemLoadConfirmMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        public override void Setup(int player_id)
        {
            if (!SaveSystemManager.Instance.CurrentRunAlreadySaved)
            {
                AddLabel("Overwrite currently loaded save?");
                AddInfo(SaveSystemManager.Instance.CurrentRunName);
                AddInfo(SaveSystemManager.Instance.CurrentRunDateTime);
                New<SpacerElement>();
            }
            this.AddButton("Confirm loading", (Action<int>)(i => this.LoadAndGoBack()));
            this.AddButton(this.Localisation["CANCEL_PROFILE"], (Action<int>)(i => this.RequestPreviousMenu()));
        }

        public void LoadAndGoBack()
        {
            SaveSystem_ModLoaderSystem.LogInfo("Loading run: " + SaveSystemMenu.currentlySelectedName);
            SaveSystemManager.Instance.LoadSave(SaveSystemMenu.currentlySelectedName);
            SaveSystemMenu.currentlySelectedName = null;
            this.RequestPreviousMenu();
        }
    }

    public class SaveSystemMenu : Menu<PauseMenuAction>
    {
        public SaveSystemMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        private static int PlayerID;
        private ButtonElement SaveButton;
        private ButtonElement LoadButton;
        private ButtonElement RenameButton;
        private ButtonElement DeleteButton;
        private Option<string> SaveSelectOption;
        private IModule SaveSelectModule;
        private LabelElement SaveSelectDescription;
        public static string currentlySelectedName;
        public static string currentlySelectedDateTime;
        private Dictionary<string, string> m_dicSavesToDatetime;

        public override void CreateSubmenus(ref Dictionary<Type, Menu<PauseMenuAction>> menus)
        {
            menus.Add(typeof(SaveSystemDeleteMenu), new SaveSystemDeleteMenu(Container, ModuleList));
            menus.Add(typeof(SaveSystemLoadConfirmMenu), new SaveSystemLoadConfirmMenu(Container, ModuleList));
        }

        public override void Setup(int player_id)
        {
            #region SaveSelect
            if (SaveSystemManager.Instance.HasSavedRuns)
            {
                InitSaveInfo();
                SaveSelectOption.OnChanged += (EventHandler<string>)((_, f) =>
                {
                    currentlySelectedName = f;
                    SaveSelectDescription.SetLabel(m_dicSavesToDatetime[currentlySelectedName]);
                    SetLoadButtonText();
                });
            }
            #endregion

            //AddLabel("Save System");
            if (SaveSystemManager.Instance.CurrentRunAlreadySaved)
                SaveButton = AddButton("Already saved", (Action<int>)(_ =>
                {

                }));
            else
                SaveButton = AddButton("Save now", (Action<int>)(_ =>
                {
                    PlayerID = player_id;
                    if (!SaveSystemManager.Instance.CurrentRunHasPreviousSaves)
                        TextInputView.RequestTextInput("Enter save name:", /*TODO: Preset with franchise name*/"", 30, new Action<TextInputView.TextInputState, string>(SaveRun));
                    else
                        SaveRun();
                    this.RequestAction(PauseMenuAction.CloseMenu);
                }));

            if (GameInfo.CurrentScene != SceneType.Kitchen && SaveSystemManager.Instance.HasSavedRuns)
            {
                New<SpacerElement>();
                SaveSelectModule = AddSelect<string>(SaveSelectOption);
                SaveSelectDescription = AddInfo(currentlySelectedDateTime);
                #region AlignMiddle - Not working
                //TextMeshPro labelSaveButton = (TextMeshPro)SaveButton.GetType().GetField("Label", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SaveButton);
                //Vector2 labelSaveButtonSizeDelta = labelSaveButton.GetComponent<RectTransform>().sizeDelta;
                //TextMeshPro labelSaveSelectDescription = (TextMeshPro)SaveSelectDescription.GetType().GetField("Label", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SaveSelectDescription);
                //SaveSelectDescription.SetSize(labelSaveButtonSizeDelta.x, labelSaveButtonSizeDelta.y);
                //labelSaveSelectDescription.alignment = TextAlignmentOptions.Midline;
                #endregion
                LoadButton = AddButton("", (Action<int>)(_ =>
                {
                    RequestSubMenu(typeof(SaveSystemLoadConfirmMenu));
                }));
                SetLoadButtonText();
                New<SpacerElement>();
                RenameButton = AddButton("Rename", (Action<int>)(_ => // TODO: same name not allowed
                {
                    PlayerID = player_id;
                    TextInputView.RequestTextInput("Enter new name:", currentlySelectedName, 30, new Action<TextInputView.TextInputState, string>(RenameRun));
                    this.RequestAction(PauseMenuAction.CloseMenu);
                }));
                DeleteButton = AddButton("Delete", (Action<int>)(_ =>
                {
                    RequestSubMenu(typeof(SaveSystemDeleteMenu));
                }));
            }

            New<SpacerElement>();
            AddButton(this.Localisation["MENU_BACK_SETTINGS"], (Action<int>)(i => this.RequestPreviousMenu()));
        }

        private void InitSaveInfo()
        {
            List<string> saveNames = SaveSystemManager.Instance.GetSaveNamesList();
            string preselectedName = SaveSystemManager.Instance.CurrentRunName != null ? SaveSystemManager.Instance.CurrentRunName : saveNames[0];
            currentlySelectedName = preselectedName;
            List<string> saveDisplayNames = SaveSystemManager.Instance.GetSaveDisplayNamesList();
            m_dicSavesToDatetime = saveNames.Zip(SaveSystemManager.Instance.GetSaveDateTimeNamesList(), (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
            currentlySelectedDateTime =  m_dicSavesToDatetime[currentlySelectedName];
            SaveSelectOption = new Option<string>(saveNames, preselectedName, saveDisplayNames);
        }

        private void SetLoadButtonText()
        {
            if (SaveSystemManager.Instance.CurrentRunName == currentlySelectedName)
            {
                LoadButton?.SetLabel("Already loaded");
            }
            LoadButton?.SetLabel("Load");
        }

        private void ReloadMenu(IModule _selectThis)
        {
            ModuleList.Clear();
            Setup(PlayerID);
            if (_selectThis != null)
                ModuleList.Select(_selectThis);
        }

        public void SaveRun()
        {
            SaveSystem_ModLoaderSystem.LogInfo("Saving current run with previouse one.");
            SaveSystemManager.Instance.SaveCurrentSave();
            ReloadMenu(SaveButton);
        }

        public void SaveRun(TextInputView.TextInputState _result, string _name)
        {
            if (_result != TextInputView.TextInputState.TextEntryComplete && _result != TextInputView.TextInputState.TextEntryCancelled)
                return;
            if (SaveSystemManager.Instance.SaveAlreadyExists(_name))
            {
                SaveSystem_ModLoaderSystem.LogWarning("Save: " + _name + " already exists!\n skipping saving.");
                ReloadMenu(SaveButton);
                return;
            }
            SaveSystem_ModLoaderSystem.LogInfo("Saving current run: " + _name);
            SaveSystemManager.Instance.SaveCurrentSave(_name);
            ReloadMenu(SaveButton);
        }

        public void RenameRun(TextInputView.TextInputState _result, string _name)
        {
            if (_result != TextInputView.TextInputState.TextEntryComplete && _result != TextInputView.TextInputState.TextEntryCancelled)
                return;
            SaveSystem_ModLoaderSystem.LogInfo("Renaming current run: " + currentlySelectedName + " to: " + _name);
            SaveSystemManager.Instance.RenameSave(currentlySelectedName, _name);
            RequestSubMenu(this.GetType(), true); // Doesnt work for some reason
        }
    }
    #endregion

    //[HarmonyPatch(typeof(DisplayVersion), "Awake")]
    //public static class DisplayVersionPatch
    //{
    //    // ReSharper disable once UnusedMember.Local
    //    static void Postfix(ref DisplayVersion __instance)
    //    {
    //        if (Session.CurrentGameNetworkMode != GameNetworkMode.Host || GameInfo.CurrentScene != SceneType.Franchise)
    //            return;
    //        SaveSystemMod.ShowVersionSave = __instance;
    //        SaveSystemMod.ShowVersionSaveDefaultText = __instance.Text.text;
    //        SaveSystem.SaveSystem.ReloadSaveSystem();
    //        __instance.Text.text = SaveSystemMod.ShowVersionSaveDefaultText + "\n Selected Save:\n" + SaveSystem.SaveSystem.SelectedSaveSlotDisplayName;
    //    }
    //}
}
