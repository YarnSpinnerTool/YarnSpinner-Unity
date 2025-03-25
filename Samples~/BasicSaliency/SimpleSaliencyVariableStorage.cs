namespace Yarn.Unity.Samples {

    using Yarn.Unity;

    /// <summary>
    /// Day
    /// </summary>
    /// <remarks>
    /// Automatically generated from Yarn project at Assets/Samples/BasicSaliency/BasicSaliency.yarnproject.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public enum Day {

        /// <summary>
        /// Monday
        /// </summary>
        Monday = 0,

        /// <summary>
        /// Tuesday
        /// </summary>
        Tuesday = 1,

        /// <summary>
        /// Wednesday
        /// </summary>
        Wednesday = 2,
    }

    /// <summary>
    /// TimeOfDay
    /// </summary>
    /// <remarks>
    /// Automatically generated from Yarn project at Assets/Samples/BasicSaliency/BasicSaliency.yarnproject.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public enum TimeOfDay {

        /// <summary>
        /// Morning
        /// </summary>
        Morning = 0,

        /// <summary>
        /// Evening
        /// </summary>
        Evening = 1,
    }

    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    internal static class SimpleSaliencyVariableStorageTypeExtensions {
        internal static int GetBackingValue(this Day enumValue) {
            switch (enumValue) {
                    case Day.Monday:
                        return 0;
                    case Day.Tuesday:
                        return 1;
                    case Day.Wednesday:
                        return 2;
                    default:
                throw new System.ArgumentException($"{enumValue} is not a valid enum case.");
            }
        }
        internal static int GetBackingValue(this TimeOfDay enumValue) {
            switch (enumValue) {
                    case TimeOfDay.Morning:
                        return 0;
                    case TimeOfDay.Evening:
                        return 1;
                    default:
                throw new System.ArgumentException($"{enumValue} is not a valid enum case.");
            }
        }
    }
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public partial class SimpleSaliencyVariableStorage : Yarn.Unity.InMemoryVariableStorage, Yarn.Unity.IGeneratedVariableStorage {
        // Accessor for Day $day
        public Day Day {
            get => this.GetEnumValueOrDefault<Day>("$day");
            set => this.SetValue("$day", value.GetBackingValue());
        }

        // Accessor for TimeOfDay $time
        public TimeOfDay Time {
            get => this.GetEnumValueOrDefault<TimeOfDay>("$time");
            set => this.SetValue("$time", value.GetBackingValue());
        }

    }
}
