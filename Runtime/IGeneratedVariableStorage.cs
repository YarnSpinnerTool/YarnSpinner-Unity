using System;
using Yarn.Utility;

namespace Yarn.Unity
{

    /// <summary>
    /// Contains methods that allow generated variable storage classes to
    /// provide information about their variables.
    /// </summary>
    public interface IGeneratedVariableStorage : Yarn.IVariableStorage {
        public string GetStringValueForEnumCase<T>(T enumCase) where T : System.Enum;
    }

    public static class GeneratedVariableStorageExtensions
    {

        /// <summary>
        /// Gets a value for the variable <paramref name="variableName"/> from
        /// <paramref name="storage"/>, or else returns the default value of
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the parameter to get a value
        /// for.</typeparam>
        /// <param name="storage">The generated variable storage class to get
        /// the value from.</param>
        /// <param name="variableName">The name of the variable to get a value
        /// for.</param>
        /// <returns>The value of <paramref name="variableName"/>, or the
        /// default value of
        /// <typeparamref name="T"/>.</returns>
        public static T GetValueOrDefault<T>(this IGeneratedVariableStorage storage, string variableName) where T : IConvertible
        {
            if (storage.TryGetValue<T>(variableName, out T result))
            {
                return result;
            }
            else
            {
                UnityEngine.Debug.Log($"Failed to get a value of type {typeof(T)} for variable {variableName}.");
                return default(T);
            }
        }

        public static void SetValue<T>(this IGeneratedVariableStorage storage, string v, T value) where T : IConvertible
        {
            switch (value)
            {
                case string stringValue:

                    storage.SetValue(v, stringValue);
                    break;
                case float floatValue:
                    storage.SetValue(v, floatValue);
                    break;
                case bool boolValue:
                    storage.SetValue(v, boolValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unhandled value type " + value.GetType());
            }
        }

        public static T GetEnumValueOrDefault<T>(this IGeneratedVariableStorage storage, string variableName) where T : System.Enum
        {
            if (!storage.TryGetValue(variableName, out object result))
            {
                UnityEngine.Debug.LogError($"Failed to get a value of type {typeof(T).Name} for variable {variableName}.");
                return default;
            }

            uint caseValue;

            if (result.GetType() == typeof(string)) {
                // Convert the string value to a hash
                caseValue = CRC32.GetChecksum((string)result);
            } else {
                caseValue = (uint)result;
            }

            if (Enum.IsDefined(typeof(T), caseValue))
            {
                return (T)Enum.ToObject(typeof(T), caseValue);
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to get a value of type {typeof(T)} for variable {variableName}: ${caseValue} is not a valid case value.");
                return default;
            }
        }

        public static void SetNumberEnum<T>(this IGeneratedVariableStorage storage, string variableName, T value) where T : System.Enum
        {
            storage.SetValue(variableName, (int)(object)value);
        }

        public static void SetStringEnum<T>(this IGeneratedVariableStorage storage, string variableName, T value) where T : System.Enum
        {
            storage.SetValue(variableName, storage.GetStringValueForEnumCase(value));
        }
    }
}
