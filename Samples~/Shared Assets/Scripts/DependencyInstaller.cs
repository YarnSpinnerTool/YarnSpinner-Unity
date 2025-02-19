
using UnityEngine;

// This script exists to make sure that both Cinemachine and the Unity Input
// System, which the samples need, are installed and configured correctly. It
// detects if certain packages are loaded, and if they're not, shows a window
// that offers an easy way to install them.
//
// You can safely delete this script from your game.

#nullable enable

namespace Yarn.Unity.Samples
{
    using System.Collections.Generic;

    [ExecuteAlways]
    public class DependencyInstaller : MonoBehaviour
    {
        public List<DependencyPackage> requirements = new();

        [System.Serializable]
        public struct DependencyPackage
        {
            public string Name;
            public string PackageName;
            public string AssemblyName;
        }
    }
}
