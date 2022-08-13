namespace Yarn.Unity;

{
    public interface IExtendedVariableStorage : Yarn.IVariableStorage
    {
        /// <summary>
        /// Returns a boolean value representing if a particular variable is
        /// inside the variable storage.
        /// </summary>
        /// <param name="variableName">The name of the variable to check
        /// for.</param>
        /// <returns><see langword="true"/> if this variable storage contains a
        /// value for the variable named <paramref name="variableName"/>; <see
        /// langword="false"/> otherwise.</returns>
        bool Contains(string variableName);

        /// <summary>
        /// Provides a unified interface for loading many variables all at once.
        /// Will override anything already in the variable storage.
        /// </summary>
        /// <param name="clear">Should the load also wipe the storage.
        /// Defaults to true so all existing variables will be cleared.
        /// </param>
        void SetAllVariables(System.Collections.Generic.Dictionary<string,float> floats, System.Collections.Generic.Dictionary<string,string> strings, System.Collections.Generic.Dictionary<string,bool> bools, bool clear = true);

        /// <summary>
        /// Provides a unified interface for exporting all variables.
        /// Intended to be a point for custom saving, editors, etc.
        /// </summary>
        (System.Collections.Generic.Dictionary<string,float>,System.Collections.Generic.Dictionary<string,string>,System.Collections.Generic.Dictionary<string,bool>) GetAllVariables();
    }
}
