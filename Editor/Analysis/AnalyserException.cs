/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;

namespace Yarn.Unity.ActionAnalyser
{
    [System.Serializable]
    public class AnalyserException : System.Exception
    {
        public AnalyserException() { }
        public AnalyserException(string message) : base(message) { }
        public AnalyserException(string message, System.Exception inner) : base(message, inner) { }
        public AnalyserException(string message, System.Exception inner, IEnumerable<Microsoft.CodeAnalysis.Diagnostic> diagnostics) : base(message, inner)
        {
            this.Diagnostics = diagnostics;
        }
        protected AnalyserException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public IEnumerable<Microsoft.CodeAnalysis.Diagnostic> Diagnostics { get; } = new List<Microsoft.CodeAnalysis.Diagnostic>();
    }
}
