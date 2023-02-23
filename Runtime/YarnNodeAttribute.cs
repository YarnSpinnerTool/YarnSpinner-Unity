using System;
using UnityEngine;


namespace Yarn.Unity
{
    /// <summary>
    /// Specifies that a field represents a reference to a named Yarn node that
    /// exists in a Yarn project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute causes the inspector to draw a popup that allows
    /// selecting a node from a list of all nodes available in a Yarn project.
    /// </para>
    /// 
    /// <para>
    /// This attribute may only be used with <see cref="string"/> fields.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class YarnNodeAttribute : PropertyAttribute {
        /// <summary>
        /// The name of a property that specifies the YarnProject containing the desired node.
        /// </summary>
        public readonly string yarnProjectAttribute;
        
        public YarnNodeAttribute(string yarnProjectAttribute)
        {
            this.yarnProjectAttribute = yarnProjectAttribute;
        }
    }
}
