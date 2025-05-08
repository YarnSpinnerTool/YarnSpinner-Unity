/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Linq;

#nullable enable

namespace Yarn.Unity.Editor
{
    [System.Serializable]
    public class FunctionInfo
    {
        public string Name = "unknown";
        public string ReturnType = "unknown";
        public string[] Parameters = System.Array.Empty<string>();

        public static FunctionInfo CreateFunctionInfoFromMethodGroup(System.Reflection.MethodInfo method)
        {
            var returnType = $"-> {method.ReturnType.Name}";

            var parameters = method.GetParameters();
            var p = new string[parameters.Count()];
            for (int i = 0; i < parameters.Count(); i++)
            {
                var q = parameters[i].ParameterType;
                p[i] = parameters[i].Name;
            }

            return new FunctionInfo
            {
                Name = method.Name,
                ReturnType = returnType,
                Parameters = p,
            };
        }
    }
}
