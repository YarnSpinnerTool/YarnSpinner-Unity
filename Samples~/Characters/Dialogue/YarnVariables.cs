namespace MyGame {

    using Yarn.Unity;

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

        // Accessor for Bool $switch_on
        /// <summary>
        /// Whether the switch is currently turned on or off.
        /// </summary>
        public bool SwitchOn {
            get => this.GetValueOrDefault<bool>("$switch_on");
            set => this.SetValue<bool>("$switch_on", value);
        }

    }
}
