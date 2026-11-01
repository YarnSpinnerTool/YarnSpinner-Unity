using UnityEngine;
using System.Collections.Generic;
#if UNITY_6000_7_OR_NEWER
    using UnityEngine.Assemblies;
#endif

namespace Yarn.Unity.Editor
{
    public static class AssemblyUtility
    {
        public static IEnumerable<System.Reflection.Assembly> GetAssemblies()
        {
#if UNITY_6000_7_OR_NEWER
            return CurrentAssemblies.GetLoadedAssemblies();
#else
            return System.AppDomain.CurrentDomain.GetAssemblies();
#endif
        }
    }
}