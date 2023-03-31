using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Kitchen;
using KitchenMods;
using SaveSystem;
using HarmonyLib;
using System.Reflection;

namespace SaveSystem_MultiMod
{
    //[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    //[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    //[UpdateBefore(typeof(PerformSaveRequests))]
    //public class SaveSystemPerformSaveRequests : GenericSystemBase, IModSystem
    //{
    //    private EntityQuery SaveRequests;
    //    private EntityQuery SaveBlockers;
    //    [ReadOnly]
    //    private EntityQuery CRenameRestaurantQuery;

    //    protected override void Initialise()
    //    {
    //        base.Initialise();
    //        SaveRequests = GetEntityQuery(typeof(CRequestSave));
    //        SaveBlockers = GetEntityQuery(new QueryHelper().Any(typeof(SPerformSceneTransition), typeof(CSceneFirstFrame)));
    //        RequireForUpdate(SaveRequests);
    //        this.CRenameRestaurantQuery = this.GetEntityQuery(ComponentType.ReadOnly<CRenameRestaurant>());
    //    }

    //    protected override void OnUpdate()
    //    {
    //        if (!SaveBlockers.IsEmpty)
    //            return;
    //        EntityManager.AddComponent<CDoNotPersist>(SaveRequests);
    //        Entity entity = SaveRequests.First();
    //        switch (SaveRequests.First<CRequestSave>().SaveType)
    //        {
    //            case SaveType.Auto:
    //                Persistence.AutoSave<PackProgressionSaveSystem>(World.EntityManager);
    //                break;
    //            case SaveType.AutoFull:
    //                Persistence.AutoSave<FullWorldSaveSystem>(World.EntityManager);
    //                Persistence.BackupWorld<WorldBackupSystem>(World.EntityManager);
    //                //Debug.LogError("AutoFull"); // StartNewDay has AutoSave, but its a direct call, and not for this system
    //                SaveSystemManager.Instance.SaveCurrentSave();
    //                SaveSystemMod.UpdateDisplayVersion();
                    
    //                break;
    //        }
    //        EntityManager.DestroyEntity(entity); // Vanilla system just returns after the first statement
    //    }
    //}

    [HarmonyPatch(typeof(Persistence), nameof(Persistence.SaveFullWorld))]
    class Persistence_Patch
    {
        [HarmonyPostfix]
        static void Postfix(int slot)
        {
            SaveSystemManager.Instance.SaveCurrentSave(slot);
        }
    }
}
