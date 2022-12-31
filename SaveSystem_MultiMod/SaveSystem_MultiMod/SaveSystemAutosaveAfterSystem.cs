//using Kitchen;
//using KitchenMods;
//using Unity.Collections;
//using Unity.Entities;
//using UnityEngine;

//namespace SaveSystem_MultiMod
//{
//    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
//    [UpdateAfter(typeof(PerformSaveRequests))]
//    public class SaveSystemAutosaveAfterSystem : GenericSystemBase, IModSystem
//    {
//        private EntityQuery SaveSystemShouldSaveRequest;

//        protected override void Initialise()
//        {
//            base.Initialise();
//            SaveSystemShouldSaveRequest = GetEntityQuery(typeof(CSaveSystemShouldSave));
//        }

//        protected override void OnUpdate()
//        {
//            if (SaveSystemShouldSaveRequest.IsEmpty)
//            {
//                return;
//            }
//            EntityManager.AddComponent<CDoNotPersist>(SaveSystemShouldSaveRequest);
//            SaveSystem_ModLoaderSystem.LogError("Trigger Auto save");
//        }
//    }
//}