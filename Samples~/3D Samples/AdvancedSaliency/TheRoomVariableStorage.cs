namespace Yarn.Unity.Samples {

    using Yarn.Unity;

    /// <summary>
    /// Character
    /// </summary>
    /// <remarks>
    /// Automatically generated from Yarn project at Assets/Samples/3D Samples/AdvancedSaliency/AdvancedSaliency.yarnproject.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public enum Character : uint {

        /// <summary>
        /// Alice
        /// </summary>
        /// <remarks>
        /// Backing value: "Alice"
        /// </remarks>
        // "Alice"
        Alice = 3863974723,

        /// <summary>
        /// Barry
        /// </summary>
        /// <remarks>
        /// Backing value: "Barry"
        /// </remarks>
        // "Barry"
        Barry = 70124160,

        /// <summary>
        /// George
        /// </summary>
        /// <remarks>
        /// Backing value: "George"
        /// </remarks>
        // "George"
        George = 3508097174,

        /// <summary>
        /// Liz
        /// </summary>
        /// <remarks>
        /// Backing value: "Liz"
        /// </remarks>
        // "Liz"
        Liz = 2184111482,
    }

    /// <summary>
    /// Scenario
    /// </summary>
    /// <remarks>
    /// Automatically generated from Yarn project at Assets/Samples/3D Samples/AdvancedSaliency/AdvancedSaliency.yarnproject.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public enum Scenario : uint {

        /// <summary>
        /// Interogation
        /// </summary>
        /// <remarks>
        /// Backing value: "Interrogation"
        /// </remarks>
        // "Interrogation"
        Interogation = 2591248492,

        /// <summary>
        /// Explore
        /// </summary>
        /// <remarks>
        /// Backing value: "Explore"
        /// </remarks>
        // "Explore"
        Explore = 2270601414,

        /// <summary>
        /// Rescue
        /// </summary>
        /// <remarks>
        /// Backing value: "Rescue"
        /// </remarks>
        // "Rescue"
        Rescue = 4259156493,

        /// <summary>
        /// Date
        /// </summary>
        /// <remarks>
        /// Backing value: "Date"
        /// </remarks>
        // "Date"
        Date = 179083332,
    }

    /// <summary>
    /// Room
    /// </summary>
    /// <remarks>
    /// Automatically generated from Yarn project at Assets/Samples/3D Samples/AdvancedSaliency/AdvancedSaliency.yarnproject.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public enum Room : uint {

        /// <summary>
        /// Office
        /// </summary>
        /// <remarks>
        /// Backing value: "Office"
        /// </remarks>
        // "Office"
        Office = 1945988660,

        /// <summary>
        /// Pub
        /// </summary>
        /// <remarks>
        /// Backing value: "Pub"
        /// </remarks>
        // "Pub"
        Pub = 1644788325,

        /// <summary>
        /// Church
        /// </summary>
        /// <remarks>
        /// Backing value: "Church"
        /// </remarks>
        // "Church"
        Church = 2539772019,

        /// <summary>
        /// Mansion
        /// </summary>
        /// <remarks>
        /// Backing value: "Mansion"
        /// </remarks>
        // "Mansion"
        Mansion = 737745662,
    }

    /// <summary>
    /// ScenarioState
    /// </summary>
    /// <remarks>
    /// Automatically generated from Yarn project at Assets/Samples/3D Samples/AdvancedSaliency/AdvancedSaliency.yarnproject.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public enum ScenarioState {

        /// <summary>
        /// NotStarted
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// Started
        /// </summary>
        Started = 1,

        /// <summary>
        /// Complete
        /// </summary>
        Complete = 2,
    }

    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    internal static class TheRoomVariableStorageTypeExtensions {
        internal static string GetBackingValue(this Character enumValue) {
            switch (enumValue) {
                    case Character.Alice:
                        return "Alice";
                    case Character.Barry:
                        return "Barry";
                    case Character.George:
                        return "George";
                    case Character.Liz:
                        return "Liz";
                    default:
                throw new System.ArgumentException($"{enumValue} is not a valid enum case.");
            }
        }
        internal static string GetBackingValue(this Scenario enumValue) {
            switch (enumValue) {
                    case Scenario.Interogation:
                        return "Interrogation";
                    case Scenario.Explore:
                        return "Explore";
                    case Scenario.Rescue:
                        return "Rescue";
                    case Scenario.Date:
                        return "Date";
                    default:
                throw new System.ArgumentException($"{enumValue} is not a valid enum case.");
            }
        }
        internal static string GetBackingValue(this Room enumValue) {
            switch (enumValue) {
                    case Room.Office:
                        return "Office";
                    case Room.Pub:
                        return "Pub";
                    case Room.Church:
                        return "Church";
                    case Room.Mansion:
                        return "Mansion";
                    default:
                throw new System.ArgumentException($"{enumValue} is not a valid enum case.");
            }
        }
        internal static int GetBackingValue(this ScenarioState enumValue) {
            switch (enumValue) {
                    case ScenarioState.NotStarted:
                        return 0;
                    case ScenarioState.Started:
                        return 1;
                    case ScenarioState.Complete:
                        return 2;
                    default:
                throw new System.ArgumentException($"{enumValue} is not a valid enum case.");
            }
        }
    }
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public partial class TheRoomVariableStorage : Yarn.Unity.InMemoryVariableStorage, Yarn.Unity.IGeneratedVariableStorage {
        // Accessor for Character $primary
        public Character Primary {
            get => this.GetEnumValueOrDefault<Character>("$primary");
            set => this.SetValue("$primary", value.GetBackingValue());
        }

        // Accessor for Character $secondary
        public Character Secondary {
            get => this.GetEnumValueOrDefault<Character>("$secondary");
            set => this.SetValue("$secondary", value.GetBackingValue());
        }

        // Accessor for Scenario $scenario
        public Scenario Scenario {
            get => this.GetEnumValueOrDefault<Scenario>("$scenario");
            set => this.SetValue("$scenario", value.GetBackingValue());
        }

        // Accessor for Room $Room
        public Room Room {
            get => this.GetEnumValueOrDefault<Room>("$Room");
            set => this.SetValue("$Room", value.GetBackingValue());
        }

        // Accessor for Bool $speak_to_primary
        public bool SpeakToPrimary {
            get => this.GetValueOrDefault<bool>("$speak_to_primary");
            set => this.SetValue<bool>("$speak_to_primary", value);
        }

        // Accessor for Bool $speak_to_secondary
        public bool SpeakToSecondary {
            get => this.GetValueOrDefault<bool>("$speak_to_secondary");
            set => this.SetValue<bool>("$speak_to_secondary", value);
        }

        // Accessor for ScenarioState $scenario_state
        public ScenarioState ScenarioState {
            get => this.GetEnumValueOrDefault<ScenarioState>("$scenario_state");
            set => this.SetValue("$scenario_state", value.GetBackingValue());
        }

    }
}
