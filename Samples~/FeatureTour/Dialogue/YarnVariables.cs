namespace MyGame {

    using Yarn.Unity;

    /// <summary>
    /// SwitchState
    /// </summary>
    /// <remarks>
    /// Automatically generated from Yarn project at Assets/Samples/FeatureTour/Dialogue/CharacterSample.yarnproject.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public enum SwitchState {

        /// <summary>
        /// Off
        /// </summary>
        Off = 0,

        /// <summary>
        /// Mid
        /// </summary>
        Mid = 1,

        /// <summary>
        /// On
        /// </summary>
        On = 2,
    }

    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    internal static class YarnVariablesTypeExtensions {
        internal static int GetBackingValue(this SwitchState enumValue) {
            switch (enumValue) {
                    case SwitchState.Off:
                        return 0;
                    case SwitchState.Mid:
                        return 1;
                    case SwitchState.On:
                        return 2;
                    default:
                throw new System.ArgumentException($"{enumValue} is not a valid enum case.");
            }
        }
    }
    [System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.0")]
    public partial class YarnVariables : Yarn.Unity.InMemoryVariableStorage, Yarn.Unity.IGeneratedVariableStorage {
        // Accessor for Number $times_interacted_with_switch
        /// <summary>
        /// The number of times we have turned the switch on or off.
        /// </summary>
        public float TimesInteractedWithSwitch {
            get => this.GetValueOrDefault<float>("$times_interacted_with_switch");
            set => this.SetValue<float>("$times_interacted_with_switch", value);
        }

        // Accessor for SwitchState $switch_state
        public SwitchState SwitchState {
            get => this.GetEnumValueOrDefault<SwitchState>("$switch_state");
            set => this.SetValue("$switch_state", value.GetBackingValue());
        }

        // Accessor for Bool $switch_on_or_mid
        public bool SwitchOnOrMid {
            get => this.GetValueOrDefault<bool>("$switch_on_or_mid");
        }

    }
}
