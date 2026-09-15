/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Yarn.Shared;

#nullable enable

namespace Yarn.Unity
{
    public static class ActionExtensions
    {
        public static NamedType ToNamedType(this Type type)
        {
            bool isUnityComponent = typeof(Component).IsAssignableFrom(type);
            var nt = new NamedType($"global::{type.FullName}", type.Name, type.FullName, YarnSpecialType.Other, isUnityComponent);
            return nt;
        }
        public static string GlobalMethodName(this MethodInfo methodInfo) // I think this is right, will have to double check that
        {
            return $"global::{methodInfo.FullMethodName()}";
        }
        public static string FullMethodName(this MethodInfo method)
        {
            return $"{method.DeclaringType.FullName}.{method.Name}";
        }
        public static YarnSpecialType ToSpecialType(this Type type)
        {
            Dictionary<Type, YarnSpecialType> basicReturns = new()
            {
                {typeof(string), YarnSpecialType.String},
                {typeof(bool), YarnSpecialType.Boolean},
                {typeof(int), YarnSpecialType.Int32},
                {typeof(uint), YarnSpecialType.UInt32},
                {typeof(short), YarnSpecialType.Int16},
                {typeof(ushort), YarnSpecialType.UInt16},
                {typeof(byte), YarnSpecialType.Byte},
                {typeof(sbyte), YarnSpecialType.SByte},
                {typeof(long), YarnSpecialType.Int64},
                {typeof(ulong), YarnSpecialType.UInt64},
                {typeof(float), YarnSpecialType.Single},
                {typeof(double), YarnSpecialType.Double},
                {typeof(decimal), YarnSpecialType.Decimal},
            };

            if (basicReturns.TryGetValue(type, out var result))
            {
                return result;
            }
            return YarnSpecialType.Other;
        }
    }

    public class Actions : IActionRegistration
    {
        private static string[] asyncReturns =
        {
            "Yarn.Unity.YarnTask",
            "System.Threading.Tasks.Task",
            "Cysharp.Threading.Tasks.UniTask",
            "UnityEngine.Awaitable",
        };
        private static Type[] asyncOpenReturns =
        {
            typeof(Yarn.Unity.YarnTask<>),
            typeof(System.Threading.Tasks.Task<>),
            typeof(Cysharp.Threading.Tasks.UniTask<>),
            typeof(UnityEngine.Awaitable<>),
        };
        private static Dictionary<Type, ReturnType> basicReturns = new()
        {
            {typeof(string), ReturnType.String},
            {typeof(bool), ReturnType.Boolean},

            {typeof(int), ReturnType.Number},
            {typeof(uint), ReturnType.Number},
            {typeof(short), ReturnType.Number},
            {typeof(ushort), ReturnType.Number},
            {typeof(byte), ReturnType.Number},
            {typeof(sbyte), ReturnType.Number},
            {typeof(long), ReturnType.Number},
            {typeof(ulong), ReturnType.Number},
            {typeof(float), ReturnType.Number},
            {typeof(double), ReturnType.Number},
            {typeof(decimal), ReturnType.Number},
        };

        public static Yarn.Shared.Action ActionFromMethodInfo(MethodInfo method, string yarnName, ActionType actionType)
        {
            List<Parameter> parameters = new();
            foreach (var paramInfo in method.GetParameters())
            {
                var p = ParameterFromInfo(paramInfo);
                parameters.Add(p);
            }

            bool isInstance = true;

            // super crude hack to make it "detect" if it is a local function or lambda
            // this isn't realistically rigorous enough but will work for now I think
            if (method.Name.Contains('<') &&  method.Name.Contains('>'))
            {
                isInstance = false;
            }
            if (method.IsStatic)
            {
                isInstance = false;
            }
            
            foreach (var art in asyncReturns)
            {
                if (method.ReturnType.FullName == art)
                {
                    return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.AsyncVoid, method.DeclaringType.ToNamedType(), parameters.ToArray());
                }
            }
            
            if (method.ReturnType == typeof(void))
            {
                return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.Void, method.DeclaringType.ToNamedType(), parameters.ToArray());
            }
            else if(basicReturns.ContainsKey(method.ReturnType))
            {
                return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, basicReturns[method.ReturnType], method.DeclaringType.ToNamedType(), parameters.ToArray());
            }
            else if (method.ReturnType.FullName == "UnityEngine.Coroutine")
            {
                return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.CoroutineVoid, method.DeclaringType.ToNamedType(), parameters.ToArray());
            }
            else if (method.ReturnType == typeof(IEnumerator))
            {
                return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.IEnumeratorVoid, method.DeclaringType.ToNamedType(), parameters.ToArray());
            }
            else
            {
                // ok so at this stage we are either an invalid action
                // or we are a generic task
                // which requires a bit more work
                // le sigh
                if (method.ReturnType.IsGenericType)
                {
                    var wrappedTypes = method.ReturnType.GetGenericArguments();
                    if (wrappedTypes.Length == 1)
                    {
                        var baseType = method.ReturnType.GetGenericTypeDefinition();
                        var internalType = wrappedTypes[0];
                        foreach (var art in asyncOpenReturns)
                        {
                            if (baseType == art)
                            {
                                if (basicReturns.ContainsKey(internalType))
                                {
                                    switch (basicReturns[internalType])
                                    {
                                        case ReturnType.String:
                                            return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.AsyncString, method.DeclaringType.ToNamedType(), parameters.ToArray());
                                        case ReturnType.Number:
                                            return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.AsyncNumber, method.DeclaringType.ToNamedType(), parameters.ToArray());
                                        case ReturnType.Boolean:
                                            return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.Boolean, method.DeclaringType.ToNamedType(), parameters.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return new InvalidAction(method.Name, actionType);
        }

        public static Parameter ParameterFromInfo(ParameterInfo parameterInfo)
        {
            // parameters are allowed to be
                // various numeric types
                // strings
                // bools
                // tokens
                    // as long as they are the last parameter
                // enums
                    // as long as they are a Yarn Enum
                // game objects
                // component derived classes
            // parameters can
                // be an array of the above types
                    // as long as it is the last parameter
                    // and not a token
                // have default values
            
            // is it an array?
            var pType = parameterInfo.ParameterType;
            if (parameterInfo.ParameterType.IsArray)
            {
                pType = parameterInfo.ParameterType.GetElementType();
            }

            // is it attributed as an enum?
            string? enumAttributeValue = parameterInfo.GetCustomAttribute<Attributes.YarnEnumParameterAttribute>()?.Name;

            // is it attributed as a node?
            var nodeAttributeType = typeof(Attributes.YarnNodeParameterAttribute);
            var isNodeAttributed = parameterInfo.GetCustomAttribute(nodeAttributeType) != null;

            // and finally does it have a default value?
            string? defaultDisplay = parameterInfo.DefaultValue?.ToString();

            Type[] basics = new Type[]
            {
                typeof(bool),
                typeof(sbyte),
                typeof(byte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(decimal),
                typeof(float),
                typeof(double),
                typeof(string),
            };
            for (int i = 0; i < basics.Length; i++)
            {
                if (basics[i] != parameterInfo.ParameterType)
                {
                    continue;
                }

                return new BasicParameter(parameterInfo.Name, basics[i].ToSpecialType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }

            if (pType == typeof(System.Threading.CancellationToken) || pType == typeof(LineCancellationToken))
            {
                return new TokenParameter(parameterInfo.Name, pType == typeof(LineCancellationToken), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }
            else if (typeof(GameObject).IsAssignableFrom(pType))
            {
                return new GameObjectParameter(parameterInfo.Name, pType.ToNamedType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }
            else if (typeof(Component).IsAssignableFrom(pType))
            {
                return new ComponentParameter(parameterInfo.Name, pType.ToNamedType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }
            else if (pType.IsEnum && pType.IsDefined(typeof(Attributes.YarnGeneratedEnumAttribute), false))
            {
                // so now we know we are an enum and flagged as a yarn generated enum
                // which means we can now trust the source gen to have written the requisite code to let us interface with this
                // but I am unsure if I can do dynamic calls like this...
                var attribute = pType.GetCustomAttribute<Attributes.YarnGeneratedEnumAttribute>();
                if (attribute == null)
                {
                    return new UnknownParameter(parameterInfo.Name, pType.ToNamedType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, null);
                }
                return new EnumParameter(parameterInfo.Name, pType.Name, attribute.backing, parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }

            return new UnknownParameter(parameterInfo.Name, pType.ToNamedType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
        }

        internal class RuntimeCommand
        {
            public RuntimeCommand(string name, Delegate @delegate, ActionType actionType)
            {
                // we make an action from the method info
                // and it's possible we can make a valid action but one we can't invoke
                // so we validate it and if it fails validation we then wipe it
                var action = Actions.ActionFromMethodInfo(@delegate.Method, name, actionType);
                if (action is not InvalidAction)
                {
                    if (!Shared.Action.TryValidateAction(action, Array.Empty<Shared.YarnConverter>(), null, false))
                    {
                        Debug.LogError("failed command validation");
                        action = new InvalidAction(action.Name, actionType);
                    }
                }

                Name = name;
                Method = @delegate.Method;
                Action = action;
                // delegates have a target which is needed for all non-instance non-static commands
                // aka locals and lambdas which are implictly capturing this
                // so we scoop up the target and keep it around for later when it comes time to invoke the method
                Target = @delegate.Target;
            }

            public RuntimeCommand(string name, MethodInfo method, ActionType actionType)
            {
                // we make an action from the method info
                // and it's possible we can make a valid action but one we can't invoke
                // so we validate it and if it fails validation we then wipe it
                var action = Actions.ActionFromMethodInfo(method, name, actionType);
                if (action is not InvalidAction)
                {
                    if (!Shared.Action.TryValidateAction(action, Array.Empty<Shared.YarnConverter>(), null, false))
                    {
                        action = new InvalidAction(action.Name, actionType);
                    }
                }

                Name = name;
                Method = method;
                Action = action;
            }

            public string Name { get; set; }
            public MethodInfo Method { get; set; }
            private Shared.Action Action;
            private object? Target { get; set; }

            private static bool TryConvertParameter(Parameter parameter, IConvertible input, Type type, out object? value)
            {
                // we know how to handle game objects, components, and basic parameters
                // for everything else we ask the collection of converters that the invoker has lying around and hope one of those can handle it
                if (parameter is GameObjectParameter)
                {
                    value = GameObject.Find(input.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return value != null;
                }
                else if (parameter is ComponentParameter cp)
                {
                    var go = GameObject.Find(input.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    if (go == null)
                    {
                        value = null;
                        return false;
                    }
                    value = go.GetComponentInChildren(type);
                    return value != null;
                }
                else if (parameter is BasicParameter bp)
                {
                    switch (bp.SpecialType)
                    {
                        case YarnSpecialType.String:
                            value = input.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            return true;

                        case YarnSpecialType.Boolean:
                            value = input.ToBoolean(System.Globalization.CultureInfo.InvariantCulture);
                            return true;

                        case YarnSpecialType.Byte:
                            {
                                value = input.ToByte(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.Decimal:
                            {
                                value = input.ToDecimal(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.Double:
                            {
                                value = input.ToDouble(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }    
                        case YarnSpecialType.Int16:
                            {
                                value = input.ToInt16(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.Int32:
                            {
                                value = input.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.Int64:
                            {
                                value = input.ToInt64(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.SByte:
                            {
                                value = input.ToSByte(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.Single:
                            {
                                value = input.ToSingle(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.UInt16:
                            {
                                value = input.ToUInt16(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.UInt32:
                            {
                                value = input.ToUInt32(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        case YarnSpecialType.UInt64:
                            {
                                value = input.ToUInt64(System.Globalization.CultureInfo.InvariantCulture);
                                return true;
                            }
                        
                        default:
                            value = null;
                            return false;
                    }
                }
                // if any of the runtime converters say they can handle it then we will let them take the wheel
                else if (ActionInvoker.TryRuntimeConvert(input, type, out value))
                {
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            private static bool TryConvertParameter(Parameter parameter, string input, Type type, out object? value)
            {
                // we know how to handle game objects, components, and basic parameters
                // for everything else we ask the collection of converters that the invoker has lying around and hope one of those can handle it
                if (parameter is GameObjectParameter)
                {
                    value = GameObject.Find(input);
                    return value != null;
                }
                else if (parameter is ComponentParameter cp)
                {
                    var go = GameObject.Find(input);
                    if (go == null)
                    {
                        value = null;
                        return false;
                    }
                    value = go.GetComponentInChildren(type);
                    return value != null;
                }
                else if (parameter is BasicParameter bp)
                {
                    switch (bp.SpecialType)
                    {
                        case YarnSpecialType.String:
                            value = input;
                            return true;

                        case YarnSpecialType.Boolean:
                            value = input == parameter.Name || input.ToLowerInvariant() == "true"; // should this be a case insenstive check?
                            return true;

                        case YarnSpecialType.Byte:
                            {
                                var result = byte.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Decimal:
                            {
                                var result = decimal.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Double:
                            {
                                var result = double.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }    
                        case YarnSpecialType.Int16:
                            {
                                var result = short.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Int32:
                            {
                                var result = int.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Int64:
                            {
                                var result = long.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.SByte:
                            {
                                var result = sbyte.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Single:
                            {
                                var result = float.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.UInt16:
                            {
                                var result = ushort.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.UInt32:
                            {
                                var result = uint.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.UInt64:
                            {
                                var result = ulong.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        
                        default:
                            value = null;
                            return false;
                    }
                }
                // if any of the runtime converters say they can handle it then we will let them take the wheel
                else if (ActionInvoker.TryRuntimeConvert(input, type, out value))
                {
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            public IDispatchResult Invoke(IConvertible[] parameters, System.Threading.CancellationToken token)
            {
                if (Action is InvalidAction)
                {
                    return new ActionDispatchFailed(StatusType.ActionUnknown);
                }

                if (Action.Type != ActionType.Function)
                {
                    return new ActionDispatchFailed(StatusType.ActionUnknown);
                }

                var (min, max) = Action.NumberOfParameters;
                if (parameters.Length < min || parameters.Length > max)
                {
                    return new ActionDispatchFailed(StatusType.InvalidParameterCount);
                }

                // if we are an instance function we need to lop off the first parameter
                ArraySegment<IConvertible> paramSlice;
                if (Action.IsInstance)
                {
                    paramSlice = new ArraySegment<IConvertible>(parameters, 1, parameters.Length - 1);
                }
                else
                {
                    // if we are a static, local, or lambda we only drop the first parameter
                    // which is the name of the command itself
                    paramSlice = new ArraySegment<IConvertible>(parameters);
                }

                object?[] resolvedParams = new object[Action.Parameters.Length];
                var parameterInfos = Method.GetParameters();

                for (int i = 0; i < Action.Parameters.Length; i++)
                {
                    var p = Action.Parameters[i];

                    if (p is TokenParameter tp)
                    {
                        resolvedParams[i] = token;
                        continue;
                    }

                    var pType = parameterInfos[i].ParameterType;
                    
                    // if we are an array we eat all the remaining values
                    if (p.IsArray)
                    {
                        // need to get the element type out of the array
                        pType = parameterInfos[i].ParameterType.GetElementType();

                        var remainingParameters = paramSlice.Count - i;
                        // if we have no parameters left in the invocation we don't need to bother doing anythign
                        // we can just make the empty array and put that in
                        if (remainingParameters < 1)
                        {
                            resolvedParams[i] = Array.CreateInstance(pType, 0);
                            continue;
                        }

                        var array = Array.CreateInstance(pType, remainingParameters);
                        for (int j = 0; j < remainingParameters; j++)
                        {
                            if (TryConvertParameter(p, paramSlice[i + j], pType, out var value))
                            {
                                array.SetValue(value, j);
                            }
                            else
                            {
                                Debug.LogError($"Unable to determine a way to convert {paramSlice[i + j]} into a {pType.Name}.");
                                return new ActionDispatchFailed(StatusType.InvalidParameter);
                            }
                        }
                        resolvedParams[i] = array;
                    }
                    else
                    {
                        // because parameters can have default values if we encounter a parameter that we don't have a matching parameter value for we flag it as being defaulted
                        // this would fail if there was a mix of default and tokens but because we require specific ordering we can trust earlier checks to have caught this
                        if (i < paramSlice.Count)
                        {
                            if (TryConvertParameter(p, paramSlice[i], pType, out var value))
                            {
                                resolvedParams[i] = value;
                            }
                            else
                            {
                                Debug.LogError($"Unable to determine a way to convert {paramSlice[i]} into a {pType.Name}.");
                                return new ActionDispatchFailed(StatusType.InvalidParameter);
                            }
                        }
                        else
                        {
                            resolvedParams[i] = System.Type.Missing;
                        }
                    }
                }

                object returnValue;
                if (Action.IsInstance)
                {
                    // we are an instance method
                    // which means we need a target and there are two possible target types
                    // either we are a component or we are converted type
                    // we check for the converted type first just in case it has a custom converter on a type that is also a mono behaviour
                    var targetName = parameters[0].ToString(System.Globalization.CultureInfo.InvariantCulture);

                    if (ActionInvoker.TryRuntimeConvert(targetName, Method.DeclaringType, out object? target))
                    {
                        returnValue = this.Method.Invoke(target, resolvedParams);
                    }
                    else if (typeof(UnityEngine.Component).IsAssignableFrom(Method.DeclaringType))
                    {
                        var go = GameObject.Find(targetName);
                        if (go == null)
                        {
                            return new ActionDispatchFailed(StatusType.NoTargetFound);
                        }
                        target = go.GetComponentInChildren(Method.DeclaringType);
                        if (target == null)
                        {
                            return new ActionDispatchFailed(StatusType.NoTargetFound);
                        }
                        returnValue = this.Method.Invoke(target, resolvedParams);
                    }
                    else
                    {
                        return new ActionDispatchFailed(StatusType.NoTargetFound);
                    }
                }
                else
                {
                    // it's static so I don't need a target
                    // I can just call it
                    returnValue = this.Method.Invoke(this.Target, resolvedParams);
                }

                return ProcessFunctionReturn(returnValue);
            }

            public IDispatchResult Invoke(string[] parameters, LineCancellationToken token, MonoBehaviour dispatcher)
            {
                if (Action is InvalidAction)
                {
                    return new ActionDispatchFailed(StatusType.ActionUnknown);
                }

                if (Action.Type != ActionType.Command)
                {
                    return new ActionDispatchFailed(StatusType.ActionUnknown);
                }

                var (min, max) = Action.NumberOfParameters;
                if (parameters.Length < min || parameters.Length > max)
                {
                    return new ActionDispatchFailed(StatusType.InvalidParameterCount);
                }

                // I think this is all I need to do to get the right number of parameters
                // we want a slice of all parameters but the first one

                // if we are an instance command we need to lop off the first two parameters
                // one for the name of the command
                // and one for the instance
                ArraySegment<string> paramSlice;
                if (Action.IsInstance)
                {
                    paramSlice = new ArraySegment<string>(parameters, 2, parameters.Length - 2);
                }
                else
                {
                    // if we are a static, local, or lambda we only drop the first parameter
                    // which is the name of the command itself
                    paramSlice = new ArraySegment<string>(parameters, 1, parameters.Length - 1);
                }
                
                object?[] resolvedParams = new object[Action.Parameters.Length];
                var parameterInfos = Method.GetParameters();

                for (int i = 0; i < Action.Parameters.Length; i++)
                {
                    var p = Action.Parameters[i];

                    if (p is TokenParameter tp)
                    {
                        if (tp.IsYarnToken)
                        {
                            resolvedParams[i] = token;
                        }
                        else
                        {
                            resolvedParams[i] = token.HurryUpToken;
                        }
                        continue;
                    }

                    var pType = parameterInfos[i].ParameterType;
                    
                    // if we are an array we eat all the remaining values
                    if (p.IsArray)
                    {
                        // need to get the element type out of the array
                        pType = parameterInfos[i].ParameterType.GetElementType();

                        var remainingParameters = paramSlice.Count - i;
                        // if we have no parameters left in the invocation we don't need to bother doing anythign
                        // we can just make the empty array and put that in
                        if (remainingParameters < 1)
                        {
                            resolvedParams[i] = Array.CreateInstance(pType, 0);
                            continue;
                        }

                        var array = Array.CreateInstance(pType, remainingParameters);
                        for (int j = 0; j < remainingParameters; j++)
                        {
                            if (TryConvertParameter(p, paramSlice[i + j], pType, out var value))
                            {
                                array.SetValue(value, j);
                            }
                            else
                            {
                                Debug.LogError($"Unable to determine a way to convert {paramSlice[i + j]} into a {pType.Name}.");
                                return new ActionDispatchFailed(StatusType.InvalidParameter);
                            }
                        }
                        resolvedParams[i] = array;
                    }
                    else
                    {
                        // because parameters can have default values if we encounter a parameter that we don't have a matching parameter value for we flag it as being defaulted
                        // this would fail if there was a mix of default and tokens but because we require specific ordering we can trust earlier checks to have caught this
                        if (i < paramSlice.Count)
                        {
                            if (TryConvertParameter(p, paramSlice[i], pType, out var value))
                            {
                                resolvedParams[i] = value;
                            }
                            else
                            {
                                Debug.LogError($"Unable to determine a way to convert {paramSlice[i]} into a {pType.Name}.");
                                return new ActionDispatchFailed(StatusType.InvalidParameter);
                            }
                        }
                        else
                        {
                            resolvedParams[i] = System.Type.Missing;
                        }
                    }
                }

                object returnValue;
                if (Action.IsInstance)
                {
                    // we are an instance method
                    // which means we need a target and there are two possible target types
                    // either we are a component or we are converted type
                    // we check for the converted type first just in case it has a custom converter on a type that is also a mono behaviour
                    var targetName = parameters[1].ToString(System.Globalization.CultureInfo.InvariantCulture);

                    if (ActionInvoker.TryRuntimeConvert(targetName, Method.DeclaringType, out object? target))
                    {
                        returnValue = this.Method.Invoke(target, resolvedParams);
                    }
                    else if (typeof(UnityEngine.Component).IsAssignableFrom(Method.DeclaringType))
                    {
                        var go = GameObject.Find(targetName);
                        if (go == null)
                        {
                            return new ActionDispatchFailed(StatusType.NoTargetFound);
                        }
                        target = go.GetComponentInChildren(Method.DeclaringType);
                        if (target == null)
                        {
                            return new ActionDispatchFailed(StatusType.NoTargetFound);
                        }
                        returnValue = this.Method.Invoke(target, resolvedParams);
                    }
                    else
                    {
                        return new ActionDispatchFailed(StatusType.NoTargetFound);
                    }
                }
                else
                {
                    // it's static so I don't need a target
                    // I can just call it
                    returnValue = this.Method.Invoke(this.Target, resolvedParams);
                }

                return ProcessCommandReturn(returnValue, dispatcher);
            }

            private static IDispatchResult ProcessFunctionReturn(object? returnValue)
            {
                if (returnValue is null)
                {
                    return new ActionDispatchFailed(StatusType.InvalidReturn);
                }

                return returnValue switch
                {
                    // are we a synchronous function, ie we already have the final value ready to be pushed into the vm
                    int st => new FunctionDispatchSucceededSynchronously(st),
                    float st => new FunctionDispatchSucceededSynchronously(st),
                    double st => new FunctionDispatchSucceededSynchronously(st),
                    sbyte st => new FunctionDispatchSucceededSynchronously(st),
                    byte st => new FunctionDispatchSucceededSynchronously(st),
                    short st => new FunctionDispatchSucceededSynchronously(st),
                    ushort st => new FunctionDispatchSucceededSynchronously(st),
                    uint st => new FunctionDispatchSucceededSynchronously(st),
                    long st => new FunctionDispatchSucceededSynchronously(st),
                    ulong st => new FunctionDispatchSucceededSynchronously(st),
                    decimal st => new FunctionDispatchSucceededSynchronously(st),
                    string st => new FunctionDispatchSucceededSynchronously(st),
                    bool st => new FunctionDispatchSucceededSynchronously(st),

                    // are we any of the myriad tasks?
                    System.Threading.Tasks.Task<int> at => new FunctionDispatchSucceededAsynchronously<int>(at),
                    System.Threading.Tasks.Task<float> at => new FunctionDispatchSucceededAsynchronously<float>(at),
                    System.Threading.Tasks.Task<double> at => new FunctionDispatchSucceededAsynchronously<double>(at),
                    System.Threading.Tasks.Task<sbyte> at => new FunctionDispatchSucceededAsynchronously<sbyte>(at),
                    System.Threading.Tasks.Task<byte> at => new FunctionDispatchSucceededAsynchronously<byte>(at),
                    System.Threading.Tasks.Task<short> at => new FunctionDispatchSucceededAsynchronously<short>(at),
                    System.Threading.Tasks.Task<ushort> at => new FunctionDispatchSucceededAsynchronously<ushort>(at),
                    System.Threading.Tasks.Task<uint> at => new FunctionDispatchSucceededAsynchronously<uint>(at),
                    System.Threading.Tasks.Task<long> at => new FunctionDispatchSucceededAsynchronously<long>(at),
                    System.Threading.Tasks.Task<ulong> at => new FunctionDispatchSucceededAsynchronously<ulong>(at),
                    System.Threading.Tasks.Task<decimal> at => new FunctionDispatchSucceededAsynchronously<decimal>(at),
                    System.Threading.Tasks.Task<string> at => new FunctionDispatchSucceededAsynchronously<string>(at),
                    System.Threading.Tasks.Task<bool> at => new FunctionDispatchSucceededAsynchronously<bool>(at),
#if USE_UNITASK
                    Cysharp.Threading.Tasks.UniTask<int> at => new FunctionDispatchSucceededAsynchronously<int>(at),
                    Cysharp.Threading.Tasks.UniTask<float> at => new FunctionDispatchSucceededAsynchronously<float>(at),
                    Cysharp.Threading.Tasks.UniTask<double> at => new FunctionDispatchSucceededAsynchronously<double>(at),
                    Cysharp.Threading.Tasks.UniTask<sbyte> at => new FunctionDispatchSucceededAsynchronously<sbyte>(at),
                    Cysharp.Threading.Tasks.UniTask<byte> at => new FunctionDispatchSucceededAsynchronously<byte>(at),
                    Cysharp.Threading.Tasks.UniTask<short> at => new FunctionDispatchSucceededAsynchronously<short>(at),
                    Cysharp.Threading.Tasks.UniTask<ushort> at => new FunctionDispatchSucceededAsynchronously<ushort>(at),
                    Cysharp.Threading.Tasks.UniTask<uint> at => new FunctionDispatchSucceededAsynchronously<uint>(at),
                    Cysharp.Threading.Tasks.UniTask<long> at => new FunctionDispatchSucceededAsynchronously<long>(at),
                    Cysharp.Threading.Tasks.UniTask<ulong> at => new FunctionDispatchSucceededAsynchronously<ulong>(at),
                    Cysharp.Threading.Tasks.UniTask<decimal> at => new FunctionDispatchSucceededAsynchronously<decimal>(at),
                    Cysharp.Threading.Tasks.UniTask<string> at => new FunctionDispatchSucceededAsynchronously<string>(at),
                    Cysharp.Threading.Tasks.UniTask<bool> at => new FunctionDispatchSucceededAsynchronously<bool>(at),
#endif

#if UNITY_2023_1_OR_NEWER
                    UnityEngine.Awaitable<int> at => new FunctionDispatchSucceededAsynchronously<int>(at),
                    UnityEngine.Awaitable<float> at => new FunctionDispatchSucceededAsynchronously<float>(at),
                    UnityEngine.Awaitable<double> at => new FunctionDispatchSucceededAsynchronously<double>(at),
                    UnityEngine.Awaitable<sbyte> at => new FunctionDispatchSucceededAsynchronously<sbyte>(at),
                    UnityEngine.Awaitable<byte> at => new FunctionDispatchSucceededAsynchronously<byte>(at),
                    UnityEngine.Awaitable<short> at => new FunctionDispatchSucceededAsynchronously<short>(at),
                    UnityEngine.Awaitable<ushort> at => new FunctionDispatchSucceededAsynchronously<ushort>(at),
                    UnityEngine.Awaitable<uint> at => new FunctionDispatchSucceededAsynchronously<uint>(at),
                    UnityEngine.Awaitable<long> at => new FunctionDispatchSucceededAsynchronously<long>(at),
                    UnityEngine.Awaitable<ulong> at => new FunctionDispatchSucceededAsynchronously<ulong>(at),
                    UnityEngine.Awaitable<decimal> at => new FunctionDispatchSucceededAsynchronously<decimal>(at),
                    UnityEngine.Awaitable<string> at => new FunctionDispatchSucceededAsynchronously<string>(at),
                    UnityEngine.Awaitable<bool> at => new FunctionDispatchSucceededAsynchronously<bool>(at),
#endif

                    YarnTask<int> at => new FunctionDispatchSucceededAsynchronously<int>(at),
                    YarnTask<float> at => new FunctionDispatchSucceededAsynchronously<float>(at),
                    YarnTask<double> at => new FunctionDispatchSucceededAsynchronously<double>(at),
                    YarnTask<sbyte> at => new FunctionDispatchSucceededAsynchronously<sbyte>(at),
                    YarnTask<byte> at => new FunctionDispatchSucceededAsynchronously<byte>(at),
                    YarnTask<short> at => new FunctionDispatchSucceededAsynchronously<short>(at),
                    YarnTask<ushort> at => new FunctionDispatchSucceededAsynchronously<ushort>(at),
                    YarnTask<uint> at => new FunctionDispatchSucceededAsynchronously<uint>(at),
                    YarnTask<long> at => new FunctionDispatchSucceededAsynchronously<long>(at),
                    YarnTask<ulong> at => new FunctionDispatchSucceededAsynchronously<ulong>(at),
                    YarnTask<decimal> at => new FunctionDispatchSucceededAsynchronously<decimal>(at),
                    YarnTask<string> at => new FunctionDispatchSucceededAsynchronously<string>(at),
                    YarnTask<bool> at => new FunctionDispatchSucceededAsynchronously<bool>(at),

                    _ => new ActionDispatchFailed(StatusType.InvalidReturn),
                };
            }

            private static IDispatchResult ProcessCommandReturn(object? returnValue, MonoBehaviour dispatcher)
            {
                if (returnValue is Coroutine coro)
                {
                    // The method returned a Coroutine object.
                    return new CommandDispatchSucceededAsynchronously(dispatcher.WaitForCoroutine(coro));
                }
                else if (returnValue is YarnTask yarnTask)
                {
                    return new CommandDispatchSucceededAsynchronously(yarnTask);
                }
#if UNITY_2023_1_OR_NEWER
                else if (returnValue is Awaitable awaitable)
                {
                    // The method returned an Awaitable. Convert it to a
                    // YarnTask. (Awaitables implement IEnumerator, so check
                    // this before testing against other IEnumerators like
                    // coroutines.)
                    return new CommandDispatchSucceededAsynchronously(awaitable);
                }
#endif
                else if (returnValue is System.Threading.Tasks.Task task)
                {
                    // The method returned a task. Convert it to a YarnTask.
                    return new CommandDispatchSucceededAsynchronously(task);
                }
#if USE_UNITASK
                else if (returnValue is Cysharp.Threading.Tasks.UniTask unitask)
                {
                    // The method returned a UniTask. Convert it to a YarnTask.
                    return new CommandDispatchSucceededAsynchronously(unitask);
                }
#endif
                else if (returnValue is IEnumerator enumerator)
                {
                    // The method returned an IEnumerator.
                    return new CommandDispatchSucceededAsynchronously(dispatcher.WaitForCoroutine(enumerator));
                }
                else
                {
                    // The method returned no value.
                    return new CommandDispatchSucceededSynchronously();
                }
            }
        }

        private Dictionary<string, RuntimeCommand> _commands = new Dictionary<string, RuntimeCommand>();
        private Dictionary<string, RuntimeCommand> _functions = new Dictionary<string, RuntimeCommand>();

        public void AddCommandHandler(string commandName, Delegate handler)
        {
            if (commandName.Contains(' '))
            {
                Debug.LogError($"Failed to register command {commandName}: command names are not allowed to contain spaces.");
                return;
            }

            if (_commands.ContainsKey(commandName))
            {
                Debug.LogError($"Failed to register command {commandName}: a command by this name has already been registered.");
                return;
            }
            else
            {
#if YARN_SOURCE_GENERATION_DEBUG_LOGGING
                Debug.Log($"Registering command {commandName}");
#endif
                _commands.Add(commandName, new RuntimeCommand(commandName, handler, ActionType.Command));
            }
        }

        public void AddFunction(string name, Delegate implementation)
        {
            if (name.Contains(' '))
            {
                Debug.LogError($"Cannot add function {name}: function names are not allowed to contain spaces.");
                return;
            }

            if (_functions.ContainsKey(name))
            {
                Debug.LogError($"Cannot add function {name}: one already exists");
                return;
            }
#if YARN_SOURCE_GENERATION_DEBUG_LOGGING
            Debug.Log($"Registering command {name} from method {implementation.Method.DeclaringType.FullName}.{implementation.Method.Name}");
#endif

            _functions.Add(name, new RuntimeCommand(name, implementation, ActionType.Function));
        }
        public void AddFunction(string name, MethodInfo implementation)
        {
            if (name.Contains(' '))
            {
                Debug.LogError($"Cannot add function {name}: function names are not allowed to contain spaces.");
                return;
            }

            if (_functions.ContainsKey(name))
            {
                Debug.LogError($"Cannot add function {name}: one already exists");
                return;
            }
#if YARN_SOURCE_GENERATION_DEBUG_LOGGING
            Debug.Log($"Registering command {name} from method {implementation.Method.DeclaringType.FullName}.{implementation.Method.Name}");
#endif

            _functions.Add(name, new RuntimeCommand(name, implementation, ActionType.Function));
        }

        public void AddCommandHandler(string commandName, MethodInfo methodInfo)
        {
            if (commandName.Contains(' '))
            {
                Debug.LogError($"Failed to register command {commandName}: command names are not allowed to contain spaces.");
                return;
            }

            if (_commands.ContainsKey(commandName))
            {
                Debug.LogError($"Failed to register command {commandName}: a command by this name has already been registered.");
                return;
            }
            else
            {
                _commands.Add(commandName, new RuntimeCommand(commandName, methodInfo, ActionType.Command));
            }
        }
        public void RemoveCommandHandler(string commandName)
        {
            if (_commands.Remove(commandName) == false)
            {
                Debug.LogError($"Can't remove command {commandName}, because no command with this name is currently registered.");
            }
        }

        public void RemoveFunction(string name)
        {
            if (!_functions.ContainsKey(name))
            {
                Debug.LogError($"Cannot remove function {name}: no function with that name exists in the library");
                return;
            }
            _functions.Remove(name);
        }

        public bool CanInvoke(string functionName)
        {
            return _functions.ContainsKey(functionName);
        }
        public bool CanDispatch(string commandName)
        {
            return _commands.ContainsKey(commandName);
        }

        internal IDispatchResult DispatchCommand(string command, MonoBehaviour coroutineHost, LineCancellationToken token)
        {
            var commandPieces = new List<string>(DialogueRunner.SplitCommandText(command));

            if (commandPieces.Count == 0)
            {
                // No text was found inside the command, so we won't be able to
                // find it.
                return new ActionDispatchFailed(StatusType.ActionUnknown);
            }

            if (_commands.TryGetValue(commandPieces[0], out var registration))
            {
                return registration.Invoke(commandPieces.ToArray(), token, coroutineHost);
            }
            else
            {
                return new ActionDispatchFailed(StatusType.ActionUnknown);
            }
        }

        public async YarnTask<IConvertible> Invoke(string functionName, IConvertible[] parameters, System.Threading.CancellationToken token)
        {
            if (_functions.TryGetValue(functionName, out var function))
            {
                var result = function.Invoke(parameters, token);
                if (result is ActionDispatchFailed)
                {
                    throw new ArgumentException($"Unable to dispatch the {functionName} function");
                }

                if (result is FunctionDispatchSucceededSynchronously fds)
                {
                    return fds.Result;
                }

                // this happened to my buddy Eric
                if (result is FunctionDispatchSucceededAsynchronously<int> fda0)
                {
                    return await fda0.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<float> fda1)
                {
                    return await fda1.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<double> fda2)
                {
                    return await fda2.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<sbyte> fda3)
                {
                    return await fda3.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<byte> fda4)
                {
                    return await fda4.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<short> fda5)
                {
                    return await fda5.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<ushort> fda6)
                {
                    return await fda6.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<uint> fda7)
                {
                    return await fda7.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<long> fda8)
                {
                    return await fda8.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<ulong> fda9)
                {
                    return await fda9.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<decimal> fda10)
                {
                    return await fda10.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<string> fda11)
                {
                    return await fda11.Task;
                }
                else if (result is FunctionDispatchSucceededAsynchronously<bool> fda12)
                {
                    return await fda12.Task;
                }

                throw new ArgumentException($"Unable to correctly dispatch the {functionName} function it dispatched as a {result.GetType()}");
            }
            throw new ArgumentException($"Unable to find a function called {functionName}");
        }

        public void AddCommandHandler(string commandName, Func<object> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
    }
}
