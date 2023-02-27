using Kitchen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Collections;

namespace SaveSystem_MultiMod
{
    #region Reflection/Helper
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

        /// <summary>
        /// Gets a MethodInfo of a given class using Reflection, that has Parameters
        /// </summary>
        /// <param name="_typeOfOriginal">Type of class to find a Method on</param>
        /// <param name="_name">Name of the Method to find</param>
        /// <param name="_paramTypes">Types of parameters of the Method in right order</param>
        /// <param name="_genericT">Type of Method</param>
        /// <returns>MethodInfo if found</returns>
        public static MethodInfo GetStaticMethod(Type _typeOfOriginal, string _name, Type[] _paramTypes, Type _genericT = null)
        {
            MethodInfo retVal = _typeOfOriginal.GetMethod(_name, BindingFlags.Static, null, _paramTypes, null);
            if (_genericT != null)
            {
                retVal = retVal.MakeGenericMethod(_genericT);
            }
            return retVal;
        }

        public static string SanitizeUserInput(string _input)
        {
            if (!string.IsNullOrWhiteSpace(_input))
            {
                string s = string.Join("_", _input.Split(Path.GetInvalidFileNameChars()));
                return string.Join("_", s.Split(Path.GetInvalidPathChars()));
            }
            return _input;
        }

        public static bool IsInCardSelection => !World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(CUnlockSelectPopupOption)).IsEmpty;

        public static string GetNameplateName
        {
            get
            {
                try
                {
                    EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    EntityQuery entityQuery = entityManager.CreateEntityQuery(typeof(CRenameRestaurant));
                    Entity my = entityQuery.GetSingletonEntity();
                    CRenameRestaurant myCom = entityManager.GetComponentData<CRenameRestaurant>(my);
                    return myCom.Name.Value.ToString();
                }
                catch (Exception _ex)
                {
                    SaveSystem_ModLoaderSystem.LogError(_ex.Message);
                    return null;
                }
            }
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

        //  http://www.java2s.com/Code/CSharp/Reflection/Getsanassemblybyitsnameifitiscurrentlyloaded.htm
        /// <summary>
        /// Gets an assembly by its name if it is currently loaded
        /// </summary>
        /// <param name="Name">Name of the assembly to return</param>
        /// <returns>The assembly specified if it exists, otherwise it returns null</returns>
        public static System.Reflection.Assembly GetLoadedAssembly(string Name)
        {
            try
            {
                foreach (Assembly TempAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (TempAssembly.GetName().Name.Equals(Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return TempAssembly;
                    }
                }
                return null;
            }
            catch { throw; }
        }
    }
    #endregion
}
