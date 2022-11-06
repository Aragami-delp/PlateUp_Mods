using BepInEx;
using UnityEngine;
using System.IO;
using Kitchen;
using System.Collections.Generic;
using KitchenData;
using KitchenLib;
using KitchenLib.Utils;
using UnityHelperClass;

namespace FBX_Test
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class FBX_Test_BepInEx : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                GameObject prefab = LoadFromFileExample.GetStreamingAssetGameObject();
                GameObject go = Instantiate(prefab);
                foreach (GetProperMaterial item in go.GetComponentsInChildren<GetProperMaterial>())
                {
                    item.GetComponent<MeshRenderer>().material = MaterialUtils.GetExistingMaterial(item.m_wantedMaterialName);
                }
            }

        }
    }

    public static class LoadFromFileExample
    {
        private static AssetBundle cucumberAssetBundle;
        public static GameObject GetStreamingAssetGameObject()
        {
            if (cucumberAssetBundle == null)
                cucumberAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "testAssetBundle"));
            if (cucumberAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                return null;
            }
            return cucumberAssetBundle.LoadAsset<GameObject>("testAsset");
        }
    }
}
