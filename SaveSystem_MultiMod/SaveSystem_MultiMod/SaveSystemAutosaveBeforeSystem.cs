//using Kitchen;
//using KitchenMods;
//using Unity.Collections;
//using Unity.Entities;

//namespace SaveSystem_MultiMod
//{
//    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
//    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
//    [UpdateBefore(typeof(PerformSaveRequests))]
//    public class SaveSystemAutosaveBeforeSystem : GenericSystemBase, IModSystem, DaySy
//    {
//        private EntityQuery SaveRequests;
//        private EntityQuery SaveBlockers;

//        protected override void Initialise()
//        {
//            base.Initialise();
//            SaveRequests = GetEntityQuery(typeof(CRequestSave));
//            SaveBlockers = GetEntityQuery(new QueryHelper().Any(typeof(SPerformSceneTransition), typeof(CSceneFirstFrame)));
//            RequireForUpdate(SaveRequests);
//        }

//        protected override void OnUpdate()
//        {
//            if (!SaveBlockers.IsEmpty)
//                return;
//            EntityManager.AddComponent<CDoNotPersist>(SaveRequests);
//            switch (SaveRequests.First<CRequestSave>().SaveType)
//            {
//                case SaveType.AutoFull:
//                    World.Add<CSaveSystemShouldSave>(new CSaveSystemShouldSave());
//                    break;
//            }
//        }
//    }

//    public struct CSaveSystemShouldSave : IComponentData, IModComponent
//    {
//    }
//}
