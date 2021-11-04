using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using UnityEngine.Serialization;

namespace Yarn.Unity
{
    [HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    public class YarnProject : ScriptableObject
    {
        [NonSerialized]
        [Obsolete("Use " + nameof(YarnProgram) + " instead, and " + nameof(Compile) + " to initialize.")]
        public byte[] compiledYarnProgram;

        [SerializeField]
        [HideInInspector]
        public Localization baseLocalization;

        [SerializeField]
        [HideInInspector]
        public List<Localization> localizations = new List<Localization>();

        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("compiledYarnProgram")]
        private byte[] rawData;

        private Program cachedProgram;

        /// <summary>
        /// The current program associated with this project. If you call
        /// <see cref="Compile(byte[])"/>, this will automatically update the
        /// program with a new object.
        /// </summary>
        public Program YarnProgram
        {
            get
            {
#pragma warning disable CS0618 // Type or member is obsolete
                SyncCompiledYarnProgram();
#pragma warning restore CS0618 // Type or member is obsolete
                if (cachedProgram == null)
                {
                    cachedProgram = Program.Parser.ParseFrom(rawData);
                }
                return cachedProgram;
            }
        }

        public Localization GetLocalization(string localeCode)
        {
            // If localeCode is null, we use the base localization.
            if (localeCode == null)
            {
                return baseLocalization;
            }

            foreach (var loc in localizations)
            {
                if (loc.LocaleCode == localeCode)
                {
                    return loc;
                }
            }

            // We didn't find a localization. Fall back to the Base
            // localization.
            return baseLocalization;
        }

        [Obsolete("Don't use and remove once 2.0 is released.")]
        private void SyncCompiledYarnProgram()
        {
            if (compiledYarnProgram != null)
            {
                ResetProgram(compiledYarnProgram);
            }
        }

        private void ResetProgram(byte[] bytecode)
        {
            rawData = bytecode;
            cachedProgram = null;
        }

        /// <summary>
        /// Deserializes a compiled Yarn program from the stored bytes in
        /// this object.
        /// </summary>
        [Obsolete("Use " + nameof(YarnProgram) + " instead, and " + nameof(Compile) + " to initialize.")]
        public Program GetProgram()
        {
            SyncCompiledYarnProgram();
            return YarnProgram;
        }

        /// <summary>
        /// Compiles the program from raw byte data.
        /// </summary>
        /// <param name="rawData">The raw serialized data derived from compilation.</param>
        public void Compile(byte[] bytecode)
        {
            // we're assuming that if you're using this API, you'll no longer be using the compiled yarn program.
#pragma warning disable CS0618 // Type or member is obsolete
            if (compiledYarnProgram == null)
            {
                Debug.LogWarning($"Don't use the obsolete {nameof(compiledYarnProgram)} at the same time as the new API.");
            }
#pragma warning restore CS0618 // Type or member is obsolete

            ResetProgram(bytecode);
        }

        public static void AddYarnFunctionMethodsToLibrary(Library library, params System.Reflection.Assembly[] assemblies) {
            throw new System.NotImplementedException($"{nameof(AddYarnFunctionMethodsToLibrary)} is not currently implemented, and is a no-op.");
            
            if (assemblies.Length == 0) {
                assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            }

            // In each assembly, find all types
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetLoadableTypes())
                {
                    // Find all static public methods on each type that
                    // have the YarnFunction attribute
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    {
                        var attributes = new List<YarnFunctionAttribute>(method.GetCustomAttributes<YarnFunctionAttribute>());

                        if (attributes.Count > 0)
                        {
                            var attr = attributes[0];
                            // This method has the YarnCommand attribute!
                            var del = method.CreateDelegate(typeof(System.Delegate));

                            library.RegisterFunction(attr.FunctionName, del);
                        }
                    }
                }
            }
        }
    }

    public class YarnFunctionAttribute : System.Attribute
    {
        public string FunctionName;
    }

}
