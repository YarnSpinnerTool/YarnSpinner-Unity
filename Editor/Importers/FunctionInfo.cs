using System.Linq;

namespace Yarn.Unity.Editor
{
    [System.Serializable]
    public class FunctionInfo
    {
        public string Name;
        public string ReturnType;
        public string[] Parameters;

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
