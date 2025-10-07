using System;

namespace Yarn.Unity.Attributes
{

#nullable enable

    /// <summary>
    /// The abstract base class for all Yarn Editor attributes.
    /// </summary>
    public abstract class YarnEditorAttribute : Attribute { }

    /// <summary>
    /// Indents a property in the Unity Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class IndentAttribute : YarnEditorAttribute
    {
        /// <summary>
        /// The amount to indent this property by.
        /// </summary>
        public int indentLevel = 1;

        /// <inheritdoc cref="IndentAttribute" path="/summary"/>
        /// <param name="indentLevel"><inheritdoc cref="indentLevel" path="/summary/node()"/> </param>
        public IndentAttribute(int indentLevel = 1)
        {
            if (this.indentLevel < 0)
            {
                indentLevel = 0;
            }
            this.indentLevel = indentLevel;
        }
    }

    /// <summary>
    /// Controls whether a property is visible or not in the Unity Inspector.
    /// </summary>
    public abstract class VisibilityAttribute : YarnEditorAttribute
    {

        /// <summary>
        /// The type of test represented by <see cref="Condition"/>.
        /// </summary>
        public enum AttributeMode
        {
            /// <summary>
            /// <see cref="Condition"/> is the name of a <see langword="bool"/>
            /// variable or a reference to a <see cref="UnityEngine.Object"/>,
            /// and the test passes when the variable is <see langword="true"/>
            /// (if bool) or non-null (if an object).
            /// </summary>
            BooleanCondition,
            /// <summary>
            /// <see cref="Condition"/> is the name of an enum variable, and the
            /// test passes when the variable's value is equal to <see
            /// cref="EnumValue"/>.
            /// </summary>
            EnumEquality,
        }

        /// <summary>
        /// The type of test that <see cref="Condition"/> represents.
        /// </summary>
        public AttributeMode Mode { get; protected set; }

        /// <summary>
        /// Controls whether the property appears when the condition passes
        /// (<see langword="true"/>), or fails (<see langword="false"/>).
        /// </summary>
        public bool Invert { get; protected set; }

        /// <summary>
        /// The name of another property on the object that determines whether
        /// this property is visible or not.
        /// </summary>
        public string? Condition { get; protected set; }

        /// <summary>
        /// The value that the variable indicated by <see cref="Condition"/> is
        /// compared to.
        /// </summary>
        /// <remarks>This value is only used when <see cref="Mode"/> is <see
        /// cref="AttributeMode.EnumEquality"/>.</remarks>
        public int EnumValue { get; protected set; }
    }

    /// <summary>
    /// Shows this property only when a condition is true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ShowIfAttribute : VisibilityAttribute
    {

        /// <inheritdoc cref="ShowIfAttribute" path="/summary"/>
        /// <param name="condition"><inheritdoc cref="VisibilityAttribute.Condition" path="/summary/node()"/></param>
        public ShowIfAttribute(string condition)
        {
            this.Invert = false;
            this.Condition = condition;
            this.Mode = AttributeMode.BooleanCondition;
            this.EnumValue = default;
        }

        /// <inheritdoc cref="ShowIfAttribute" path="/summary"/>
        /// <param name="condition"><inheritdoc cref="VisibilityAttribute.Condition" path="/summary/node()"/> This variable must be an enum.</param>
        /// <param name="value"><inheritdoc cref="VisibilityAttribute.EnumValue" path="/summary/node()"/></param>
        public ShowIfAttribute(string condition, object value)
        {
            this.Invert = false;
            this.Condition = condition;
            this.Mode = AttributeMode.EnumEquality;
            this.EnumValue = (int)value;
        }
    }

    /// <summary>
    /// Hides this property when a condition is true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class HideIfAttribute : VisibilityAttribute
    {
        /// <inheritdoc cref="HideIfAttribute" path="/summary"/>
        /// <param name="condition"><inheritdoc cref="VisibilityAttribute.Condition" path="/summary/node()"/></param>
        public HideIfAttribute(string condition)
        {
            this.Invert = true;
            this.Condition = condition;
        }

        /// <inheritdoc cref="HideIfAttribute" path="/summary"/>
        /// <param name="condition"><inheritdoc cref="VisibilityAttribute.Condition" path="/summary/node()"/> This variable must be an enum.</param>
        /// <param name="value"><inheritdoc cref="VisibilityAttribute.EnumValue" path="/summary/node()"/></param>
        public HideIfAttribute(string condition, object value)
        {
            this.Invert = true;
            this.Condition = condition;
            this.Mode = AttributeMode.EnumEquality;
            this.EnumValue = (int)value;
        }
    }

    /// <summary>
    /// Shows a header above this property and the following properties that
    /// have the same group name, optionally as a foldout.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GroupAttribute : YarnEditorAttribute
    {
        /// <summary>
        /// The name of the group.
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// Whether to show this group as a fold-out.
        /// </summary>
        public bool FoldOut { get; }

        /// <inheritdoc cref="GroupAttribute" path="/summary"/>
        /// <param name="groupName"><inheritdoc cref="GroupName" path="/summary/node()"/></param>
        /// <param name="foldOut"><inheritdoc cref="FoldOut" path="/summary/node()"/></param>
        public GroupAttribute(string groupName, bool foldOut = false)
        {
            this.GroupName = groupName;
            this.FoldOut = foldOut;
        }
    }

    /// <summary>
    /// Overrides the displayed label of the property in the Unity Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LabelAttribute : YarnEditorAttribute
    {
        /// <summary>
        /// The label to show for this property.
        /// </summary>
        public string Label { get; }

        /// <inheritdoc cref="LabelAttribute" path="/summary"/>
        /// <param name="label"><inheritdoc cref="Label" path="/summary/node()"/></param>
        public LabelAttribute(string label)
        {
            this.Label = label;
        }
    }

    /// <summary>
    /// Overrides the displayed label of the property in the Unity Inspector by
    /// getting a label from a named method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LabelFromAttribute : YarnEditorAttribute
    {
        /// <summary>
        /// The method to invoke that will return the label to display. The
        /// method must be an instance method, take no parameters, and return a
        /// <see cref="string"/>.
        /// </summary>
        public string SourceMethod { get; }

        /// <inheritdoc cref="LabelAttribute" path="/summary"/>
        /// <param name="methodName"><inheritdoc cref="SourceMethod"
        /// path="/summary/node()"/></param>
        public LabelFromAttribute(string methodName)
        {
            this.SourceMethod = methodName;
        }
    }

    /// <summary>
    /// Shows an error message box if this property is null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MustNotBeNullAttribute : YarnEditorAttribute
    {
        /// <summary>
        /// The text of the error message to show if the property is null. If
        /// not provided, a generic error message is shown.
        /// </summary>
        public string? Label { get; }

        /// <inheritdoc cref="MustNotBeNullAttribute" path="/summary"/>
        /// <param name="label"><inheritdoc cref="Label" path="/summary/node()"/></param>
        public MustNotBeNullAttribute(string? label = null)
        {
            this.Label = label;
        }
    }

    /// <summary>
    /// Shows an error message box if this property is null and the variable
    /// indicated by <see cref="Condition"/> is false.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MustNotBeNullWhenAttribute : YarnEditorAttribute
    {
        /// <summary>
        /// The name of another property on this object to compare to. This
        /// variable must be either a boolean value, or a <see
        /// cref="UnityEngine.Object"/> reference.
        /// </summary>
        public string Condition { get; }

        /// <summary>
        /// The text of the error message to show if the property is null and
        /// the condition is met. If not provided, a generic error message is
        /// shown.
        /// </summary>
        public string? Label { get; }

        /// <inheritdoc cref="MustNotBeNullWhenAttribute" path="/summary"/>
        /// <param name="condition"><inheritdoc cref="Condition" path="/summary/node()"/></param>
        /// <param name="label"><inheritdoc cref="Label" path="/summary/node()"/></param>
        public MustNotBeNullWhenAttribute(string condition, string? label = null)
        {
            this.Condition = condition;
            this.Label = label;
        }
    }

    /// <summary>
    /// Shows a message box on the property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class MessageBoxAttribute : YarnEditorAttribute
    {
        /// <summary>
        /// The name of a method that will be called to determine the contents
        /// of the message box. The method must return a <see cref="Message"/>.
        /// </summary>
        public string SourceMethod { get; }

        /// <summary>
        /// The type of the message box to display.
        /// </summary>
        /// <seealso cref="UnityEditor.MessageType"/>
        public enum Type
        {
            /// <summary>
            /// Do not show an icon in the message box.
            /// </summary>
            /// <seealso cref="UnityEditor.MessageType.None"/>
            None,
            /// <summary>
            /// Show an information icon in the message box.
            /// </summary>
            /// <seealso cref="UnityEditor.MessageType.Info"/>
            Info,
            /// <summary>
            /// Show a warning icon in the message box.
            /// </summary>
            /// <seealso cref="UnityEditor.MessageType.Warning"/>
            Warning,
            /// <summary>
            /// Show an error icon in the message box.
            /// </summary>
            /// <seealso cref="UnityEditor.MessageType.Error"/>
            Error
        };

        /// <summary>
        /// A description for a message box to show in the Unity Inspector.
        /// </summary>
        public struct Message
        {
            /// <summary>
            /// The type of the message to show.
            /// </summary>
            public Type type;

            /// <summary>
            /// The text to show in the message box. If this value is <see
            /// langword="null"/>, no message box is displayed.
            /// </summary>
            public string? text;

            /// <summary>
            /// Creates a new message with the given string.
            /// </summary>
            /// <param name="text"><inheritdoc cref="text" path="/summary/node()"/></param>
            public static implicit operator Message(string? text)
            {
                return new Message
                {
                    type = Type.Error,
                    text = text,
                };
            }
        }

        /// <inheritdoc cref="MessageBoxAttribute" path="/summary"/>
        /// <param name="sourceMethod"><inheritdoc cref="SourceMethod" path="/summary/node()"/></param>
        public MessageBoxAttribute(string sourceMethod)
        {
            this.SourceMethod = sourceMethod;
        }

        /// <summary>
        /// Creates a new error <see cref="Message"/> using the provided text.
        /// </summary>
        /// <param name="message"><inheritdoc cref="Message.text" path="/summary/node()"/></param>
        /// <returns>A new <see cref="Message"/>.</returns>
        public static Message Error(string message)
        {
            return new Message { type = Type.Error, text = message };
        }

        /// <summary>
        /// Creates a new warning <see cref="Message"/> using the provided text.
        /// </summary>
        /// <inheritdoc cref="Error" path="/param"/>
        /// <inheritdoc cref="Error" path="/returns"/>
        public static Message Warning(string message)
        {
            return new Message { type = Type.Warning, text = message };
        }

        /// <summary>
        /// Creates a new information <see cref="Message"/> using the provided text.
        /// </summary>
        /// <inheritdoc cref="Error" path="/param"/>
        /// <inheritdoc cref="Error" path="/returns"/>
        public static Message Info(string message)
        {
            return new Message { type = Type.Info, text = message };
        }

        /// <summary>
        /// Creates a new neutral <see cref="Message"/> using the provided text.
        /// </summary>
        /// <inheritdoc cref="Error" path="/param"/>
        /// <inheritdoc cref="Error" path="/returns"/>
        public static Message Neutral(string message)
        {
            return new Message { type = Type.None, text = message };
        }

        /// <summary>
        /// Returns a new <see cref="Message"/> that represents an instruction
        /// to not draw a message box at all.
        /// </summary>
        public static Message NoMessage => new Message { type = Type.None, text = null };
    }
}
