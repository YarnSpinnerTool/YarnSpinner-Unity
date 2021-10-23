using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Yarn.Unity
{

    [HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    public class YarnProject : ScriptableObject
    {

        [SerializeField]
        [HideInInspector]
        public byte[] compiledYarnProgram;

        [SerializeField]
        [HideInInspector]
        public Localization baseLocalization;

        [SerializeField]
        [HideInInspector]
        public List<Localization> localizations = new List<Localization>();

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

        /// <summary>
        /// Deserializes a compiled Yarn program from the stored bytes in
        /// this object.
        /// </summary>
        public Program GetProgram()
        {
            return Program.Parser.ParseFrom(compiledYarnProgram);
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
