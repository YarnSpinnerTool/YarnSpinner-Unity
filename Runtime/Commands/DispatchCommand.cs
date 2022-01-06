using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Yarn.Unity
{
    using Injector = Func<string, object>;
    using Converter = Func<string, object>;

    internal class DispatchCommand
    {
        public MethodInfo Method { get; set; }
        public Injector Injector { get; set; }
        public Converter[] Converters { get; set; }

        public bool TryInvoke(string[] args, out object returnValue)
        {
            returnValue = null;

            // if the method isn't static, but doesn't have an object name,
            // then we can't proceed, but it might be caught by a manually
            // registered function.
            if (!Method.IsStatic && args.Length < 2) { return false; }

            try
            {
                var instance = Method.IsStatic ? null : Injector?.Invoke(args[1]);
                var finalArgs = ActionManager.ParseArgs(Method, Converters, args, Method.IsStatic);
                returnValue = Method.Invoke(instance, finalArgs);
                return true;
            }
            catch (Exception e) when (
                e is ArgumentException // when arguments are invalid
                || e is TargetException // when a method is not static, but the instance ended up null
            )
            {
                Debug.LogError($"Can't run command {args[0]}: {e.Message}");
                return false;
            }
        }
    }
}
