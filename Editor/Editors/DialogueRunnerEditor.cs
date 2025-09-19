/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using UnityEditor;

#if USE_UNITY_LOCALIZATION
using UnityEngine.Localization;
#endif

using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using Yarn.Unity.UnityLocalization;
using Yarn.Unity.Attributes;

namespace Yarn.Unity.Editor
{

#nullable enable

    /// <summary>
    /// A delegate that renders a serialized property in the Inspector.
    /// </summary>
    /// <seealso cref="CustomUIForAttribute"/>
    delegate void PropertyRenderer(SerializedProperty property);

    /// <summary>
    /// An attribute that allows an editor to override the appearance of a named
    /// property in the Inspector.
    /// </summary>
    /// <remarks>
    /// When applied to a method in a <see cref="YarnEditor"/> subclass that
    /// takes a single <see cref="SerializedProperty"/> argument and returns
    /// <see langword="void"/>, the method will be invoked when the editor needs
    /// to draw UI for the property (instead of drawing the default property
    /// field.)
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CustomUIForAttribute : YarnEditorAttribute
    {
        /// <summary>
        /// The name of the property that this attribute is for.
        /// </summary>
        /// <remarks>
        /// This must match a property on a serialized object, and will be used
        /// to determine which property to render.
        /// </remarks>
        public string propertyName;

        /// <summary>
        /// Initializes a new <see cref="CustomUIForAttribute"/> with the
        /// specified property name.
        /// </summary>
        public CustomUIForAttribute(string methodName)
        {
            this.propertyName = methodName;
        }
    }

    internal static class AttributeExtensions
    {

        /// <summary>
        /// A struct that represents the result of evaluating an attribute.
        /// </summary>
        public readonly struct AttributeEvaluationResult
        {
            /// <summary>
            /// The type of result this is.
            /// </summary>
            public enum ResultType
            {
                /// <summary>
                /// The attribute was successfully evaluated to a true value.
                /// </summary>
                Passed,
                /// <summary>
                /// The attribute was successfully evaluated to a false value.
                /// </summary>
                Failed,
                /// <summary>
                /// The attribute evaluation failed with an error message.
                /// </summary>
                Error,
            }

            /// <summary>
            /// Gets or sets the type of result this is.
            /// </summary>
            public readonly ResultType Result;

            /// <summary>
            /// Gets or sets a message indicating why the evaluation failed, if
            /// it did.
            /// </summary>
            /// <remarks>This value is non-<see langword="null"/> if <see
            /// cref="Result"/> is equal to <see cref="ResultType.Error"/>.
            /// </remarks>
            public readonly string? Message;

            /// <summary>
            /// Initializes a new AttributeEvaluationResult with the specified
            /// result and message.
            /// </summary>
            private AttributeEvaluationResult(ResultType result, string? message)
            {
                this.Result = result;
                this.Message = message;
            }

            /// <summary>
            /// Implicitly converts a boolean value to an <see
            /// cref="AttributeEvaluationResult"/>.
            /// </summary>
            /// <remarks>
            /// The resulting <see cref="AttributeEvaluationResult"/> will have
            /// a <see cref="Result"/> of either <see cref="ResultType.Passed"/>
            /// or <see cref="ResultType.Failed"/>, depending on the value of
            /// <paramref name="value"/>.
            /// </remarks>
            public static implicit operator AttributeEvaluationResult(bool value)
            {
                return new AttributeEvaluationResult
                (
                    result: value ? ResultType.Passed : ResultType.Failed,
                    message: null
                );
            }

            /// <summary>
            /// Implicitly converts a string value to an <see
            /// cref="AttributeEvaluationResult"/>.
            /// </summary>
            /// <remarks>
            /// The resulting <see cref="AttributeEvaluationResult"/> will have
            /// a <see cref="Result"/> of <see cref="ResultType.Error"/>.
            /// </remarks>
            public static implicit operator AttributeEvaluationResult(string errorMessage)
            {
                return new AttributeEvaluationResult
                (
                    result: ResultType.Error,
                    message: errorMessage
                );
            }
        }

        /// <summary>
        /// Evaluates a <see cref="VisibilityAttribute"/> on a serialized
        /// object.
        /// </summary>
        /// <returns>An <see cref="AttributeEvaluationResult"/> indicating
        /// whether the attribute was successfully evaluated and
        /// passed.</returns>
        public static AttributeEvaluationResult Evaluate(this VisibilityAttribute visibilityAttribute, SerializedObject target)
        {
            if (target.targetObject == null)
            {
                return "Target object is null";
            }
            var property = target.FindProperty(visibilityAttribute.Condition);

            SerializedPropertyType propertyType;

            int enumValue = -1;
            bool booleanValue = false;
            UnityEngine.Object? objectValue = null;

            if (property != null)
            {
                // Found a serialized property on this object. Is it a type we
                // can use?
                propertyType = property.propertyType;
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        booleanValue = property.boolValue;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        objectValue = property.objectReferenceValue;
                        break;
                    case SerializedPropertyType.Enum:
                        enumValue = property.intValue;
                        break;
                    default:
                        return $"{visibilityAttribute.Condition} must be an enum value or boolean, not " + property.type;
                }
            }
            else
            {
                // Property is missing. Is there maybe a property on it by this name?
                var prop = target.targetObject.GetType().GetProperty(visibilityAttribute.Condition, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                {
                    // There is! Fetch its value, and check to see if it's a
                    // type we can use.
                    var propertyValue = prop.GetValue(target.targetObject);
                    if (propertyValue is bool booleanPropertyValue)
                    {
                        propertyType = SerializedPropertyType.Boolean;
                        booleanValue = booleanPropertyValue;
                    }
                    else if (propertyValue is UnityEngine.Object objectPropertyValue)
                    {
                        propertyType = SerializedPropertyType.ObjectReference;
                        objectValue = (UnityEngine.Object)objectPropertyValue;
                    }
                    else if (propertyValue is Enum enumPropertyValue)
                    {
                        enumValue = Convert.ToInt32(enumPropertyValue);
                        propertyType = SerializedPropertyType.Enum;
                    }
                    else
                    {
                        return $"{visibilityAttribute.Condition} must be an object reference, enum value or boolean, not " + prop.PropertyType.Name;
                    }
                }
                else
                {
                    // Failed to find a serialized property, or a C# property.
                    return $"{visibilityAttribute.Condition} not found";
                }
            }

            bool result;

            switch (visibilityAttribute.Mode)
            {
                case VisibilityAttribute.AttributeMode.BooleanCondition:
                    switch (propertyType)
                    {
                        case SerializedPropertyType.ObjectReference:
                            result = objectValue != null;
                            break;
                        case SerializedPropertyType.Boolean:
                            result = booleanValue;
                            break;
                        default:
                            // Property is an unhandled type
                            return $"{visibilityAttribute.Condition} must be a boolean or object reference, not {propertyType}";
                    }
                    break;
                case VisibilityAttribute.AttributeMode.EnumEquality:
                    if (propertyType == SerializedPropertyType.Enum)
                    {
                        result = enumValue == visibilityAttribute.EnumValue;
                    }
                    else
                    {
                        return $"{visibilityAttribute.Condition} must be an enum, not a {propertyType}";
                    }
                    break;
                default:
                    return $"Unhandled visibility attribute mode {visibilityAttribute.Mode}";
            }


            if (visibilityAttribute.Invert)
            {
                result = !result;
            }
            return result;
        }

        /// <summary>
        /// Evaluates a <see cref="MustNotBeNullAttribute"/> on a serialized
        /// property. 
        /// </summary>
        /// <returns>An <see cref="AttributeEvaluationResult"/> indicating
        /// whether the attribute was successfully evaluated and
        /// passed.</returns>
        public static AttributeEvaluationResult Evaluate(this MustNotBeNullAttribute mustNotBeNullAttribute, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return $"{property.name} must be an object reference";
            }

            return property.objectReferenceValue != null;
        }

        /// <summary>
        /// Evaluates a <see cref="MustNotBeNullWhenAttribute"/> on a serialized
        /// property. 
        /// </summary>
        /// <returns>An AttributeEvaluationResult indicating whether the
        /// attribute was successfully evaluated and passed.</returns>
        public static AttributeEvaluationResult Evaluate(this MustNotBeNullWhenAttribute mustNotBeNullWhenAttribute, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return $"{property.name} must be an object reference";
            }

            bool ruleApplies;

            var targetProperty = property.serializedObject.FindProperty(mustNotBeNullWhenAttribute.Condition);

            if (targetProperty == null)
            {
                return $"Unknown property {mustNotBeNullWhenAttribute.Condition}";
            }

            switch (targetProperty.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    ruleApplies = targetProperty.objectReferenceValue != null;
                    break;
                case SerializedPropertyType.Boolean:
                    ruleApplies = targetProperty.boolValue;
                    break;
                default:
                    // Property is an unhandled type
                    return $"{mustNotBeNullWhenAttribute.Condition} must be a boolean or object reference, not {targetProperty.propertyType}";
            }

            if (!ruleApplies)
            {
                // The rule doesn't apply, so indicate that we're a-ok
                return true;
            }

            return property.objectReferenceValue != null;
        }

        /// <summary>
        /// Gets information for showing a message box from a
        /// MessageBoxAttribute on a serialized object.
        /// </summary>
        /// <returns>A <see cref="MessageBoxAttribute.Message"/> with the text
        /// and type of the message box.
        /// </returns>
        public static MessageBoxAttribute.Message GetMessage(this MessageBoxAttribute messageBoxAttribute, SerializedObject serializedObject)
        {
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return "Serialized object is null";
            }
            var methodName = messageBoxAttribute.SourceMethod;
            if (serializedObject.isEditingMultipleObjects)
            {
                // If we're editing multiple objects, don't show a message box
                return null;
            }
            var target = serializedObject.targetObject;
            var method = serializedObject.targetObject.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                return $@"Failed to find an instance method ""{methodName}"" on this object";
            }
            if (method.ReturnType != typeof(MessageBoxAttribute.Message))
            {
                return $@"Method ""{methodName}"" must return a {nameof(MessageBoxAttribute.Message)}";
            }
            if (method.GetParameters().Length != 0)
            {
                return $@"Method ""{methodName}"" must not accept any parameters";
            }
            try
            {
                var result = method.Invoke(target, Array.Empty<object>());
                if (result is MessageBoxAttribute.Message message)
                {
                    return message;
                }
                else
                {
                    return $@"Method ""{methodName}"" did not return a valid message";
                }
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException);
                return $@"Method ""{methodName}"" threw a {e.InnerException.GetType().Name}: {e.InnerException.Message ?? "(no message)"}";
            }
            catch (Exception e)
            {
                Debug.LogException(e, target);
                return $@"{e.GetType().Name} thrown when calling ""{methodName}"": {e.Message ?? "(no message)"}";
            }
        }
        /// <summary>
        /// Gets information for showing a message box from a
        /// MessageBoxAttribute on a serialized object.
        /// </summary>
        /// <returns>A <see cref="MessageBoxAttribute.Message"/> with the text
        /// and type of the message box.
        /// </returns>
        public static string GetLabel(this LabelFromAttribute messageBoxAttribute, SerializedObject serializedObject)
        {
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return "Serialized object is null";
            }
            var methodName = messageBoxAttribute.SourceMethod;
            if (serializedObject.isEditingMultipleObjects)
            {
                // If we're editing multiple objects, show only a placeholder
                return "-";
            }
            var target = serializedObject.targetObject;
            var method = serializedObject.targetObject.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                return $@"Failed to find an instance method ""{methodName}"" on this object";
            }
            if (method.ReturnType != typeof(string))
            {
                return $@"Method ""{methodName}"" must return a string";
            }
            if (method.GetParameters().Length != 0)
            {
                return $@"Method ""{methodName}"" must not accept any parameters";
            }
            try
            {
                var result = method.Invoke(target, Array.Empty<object>());
                if (result is string message)
                {
                    return message;
                }
                else
                {
                    return $@"Method ""{methodName}"" did not return a valid message";
                }
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException);
                return $@"Method ""{methodName}"" threw a {e.InnerException.GetType().Name}: {e.InnerException.Message ?? "(no message)"}";
            }
            catch (Exception e)
            {
                Debug.LogException(e, target);
                return $@"{e.GetType().Name} thrown when calling ""{methodName}"": {e.Message ?? "(no message)"}";
            }
        }
    }

    /// <summary>
    /// Contains information about a property in a serialized object relevant to
    /// the Yarn Spinner attribute system.
    /// </summary>
    readonly struct PropertyInfo
    {
        /// <summary>
        /// A property on a <see cref="SerializedObject"/>.
        /// </summary>
        public readonly SerializedProperty serializedProperty;

        /// <summary>
        /// The collection of Yarn Editor attributes on the field that <see
        /// cref="serializedProperty"/> represents.
        /// </summary>
        public readonly YarnEditorAttribute[] attributes;

        /// <summary>
        /// The field that <see cref="serializedProperty"/> represents.
        /// </summary>
        private readonly FieldInfo? field;

        /// <summary>
        /// Gets the collection of all attributes associated with the field that
        /// a <see cref="SerializedProperty"/> represents.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static IEnumerable<Attribute> GetAttributes(SerializedProperty property)
        {
            // The script property doesn't correspond to a field on the target
            // object, so we won't find one when we ask for it. In this
            // situation, always return an empty collection if we're getting a
            // property with that name
            if (property.name == YarnEditor.ScriptPropertyName)
            {
                return Array.Empty<Attribute>();
            }

            // Attempt to find the field that backs the property
            FieldInfo? field = GetField(property);
            if (field != null)
            {
                // We found the field; get all custom attributes from it
                return field.GetCustomAttributes();
            }
            else
            {
                // We didn't find it. This is generally an error; we'll complain
                // about it and return an empty collection of attributes so that
                // we don't break the Inspector.
                Debug.LogWarning($"Failed to find field {property.name} on object {property.serializedObject.targetObject.name}");
                return Array.Empty<Attribute>();
            }
        }

        private static FieldInfo? GetField(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            var field = target.GetType().GetField(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field;
        }

        /// <summary>
        /// Initialises a new <see cref="PropertyInfo"/> given a serialized
        /// property.
        /// </summary>
        /// <param name="property">The serialized property to create the <see
        /// cref="PropertyInfo"/> from.</param>
        public PropertyInfo(SerializedProperty property)
        {
            this.serializedProperty = property;
            this.attributes = GetAttributes(property).OfType<YarnEditorAttribute>().ToArray();
            this.field = GetField(property);
        }

        /// <summary>
        /// Get a value indicating whether <see cref="serializedProperty"/> was
        /// declared on a parent type of its <see
        /// cref="SerializedProperty.serializedObject"/>.
        /// </summary>
        public bool IsInherited
        {
            get
            {
                if (this.field == null)
                {
                    return false;
                }
                var owner = this.serializedProperty.serializedObject.targetObject;
                var ownerType = owner.GetType();
                var declaringType = this.field.DeclaringType;
                return ownerType == declaringType;
            }
        }
    }

    /// <summary>
    /// A custom editor that makes use of <see cref="YarnEditorAttribute"/>
    /// attributes to control the appearance of variables in the Inspector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To use this editor for your classes, create a subclass of it and use the
    /// <see cref="CustomEditor"/> attribute to mark it as the editor for your
    /// type. The Yarn Editor attributes will then start working in the
    /// Inspector for your objects.
    /// </para>
    /// </remarks>
    public abstract class YarnEditor : UnityEditor.Editor
    {
        internal const string ScriptPropertyName = "m_Script";

        private string? currentGroup;

        private Dictionary<string, PropertyInfo> propertyInfos = new Dictionary<string, PropertyInfo>();

        private Dictionary<string, PropertyRenderer> customPropertyRenderers = new Dictionary<string, PropertyRenderer>();

        private List<(string Message, MessageType Type)> messageBoxes = new List<(string, MessageType)>();

        /// <summary>
        /// Draws a single property in the Inspector.
        /// </summary>
        /// <param name="property"></param>
        private void DrawPropertyField(PropertyInfo property)
        {

            int indentation = 0;
            GroupAttribute? group = null;
            string? label = null;
            this.messageBoxes.Clear();

            // Get all relevant attributes for this property and get information
            // from it.
            foreach (var attr in property.attributes)
            {
                AttributeExtensions.AttributeEvaluationResult result;
                switch (attr)
                {
                    case VisibilityAttribute visibilityAttribute:
                        result = visibilityAttribute.Evaluate(property.serializedProperty.serializedObject);

                        if (result.Result == AttributeExtensions.AttributeEvaluationResult.ResultType.Failed)
                        {
                            // A visibility attribute has indicated that we
                            // shouldn't show the field, so exit from this
                            // method early and don't draw the property.
                            return;
                        }
                        break;
                    case IndentAttribute indentAttribute:
                        indentation = indentAttribute.indentLevel;
                        result = true;
                        break;
                    case GroupAttribute groupAttribute:
                        group = groupAttribute;
                        result = true;
                        break;
                    case LabelAttribute labelAttribute:
                        label = labelAttribute.Label;
                        result = true;
                        break;
                    case MustNotBeNullAttribute mustNotBeNullAttribute:
                        result = mustNotBeNullAttribute.Evaluate(property.serializedProperty);
                        if (result.Result == AttributeExtensions.AttributeEvaluationResult.ResultType.Failed)
                        {
                            messageBoxes.Add((mustNotBeNullAttribute.Label ?? $"{ObjectNames.NicifyVariableName(property.serializedProperty.name)} must not be null", MessageType.Error));
                        }
                        break;
                    case MustNotBeNullWhenAttribute mustNotBeNullWhenAttribute:
                        result = mustNotBeNullWhenAttribute.Evaluate(property.serializedProperty);
                        if (result.Result == AttributeExtensions.AttributeEvaluationResult.ResultType.Failed)
                        {
                            messageBoxes.Add((mustNotBeNullWhenAttribute.Label
                                ?? $"{ObjectNames.NicifyVariableName(property.serializedProperty.name)} must not be " +
                                    $"null when {ObjectNames.NicifyVariableName(mustNotBeNullWhenAttribute.Condition)} is set",
                                MessageType.Error));
                        }
                        break;
                    case MessageBoxAttribute messageBoxAttribute:
                        MessageBoxAttribute.Message message = messageBoxAttribute.GetMessage(property.serializedProperty.serializedObject);
                        if (message.text != null)
                        {
                            messageBoxes.Add((message.text, message.type switch
                            {
                                MessageBoxAttribute.Type.Info => MessageType.Info,
                                MessageBoxAttribute.Type.Warning => MessageType.Warning,
                                MessageBoxAttribute.Type.Error => MessageType.Error,
                                _ => MessageType.None
                            }));
                        }
                        result = true;
                        break;
                    case LabelFromAttribute labelFromAttribute:
                        label = labelFromAttribute.GetLabel(property.serializedProperty.serializedObject);
                        result = true;
                        break;
                    default:
                        result = $"Unknown attribute {attr.GetType()}";
                        break;
                }

                if (result.Result == AttributeExtensions.AttributeEvaluationResult.ResultType.Error)
                {
                    messageBoxes.Add((result.Message ?? "Unknown error", MessageType.Error));
                }
            }

            // Gets a unique string ID for a given group on a specific object.
            string GetGroupID(GroupAttribute group)
            {
                var target = property.serializedProperty.serializedObject.targetObject;
                var uniqueGroupID = $"{target.GetType()}_{target.GetInstanceID()}_group_{group.GroupName}";
                return uniqueGroupID;
            }

            // Renders the header of a group. If the group is a foldout, renders
            // the header and manages its state.
            void StartGroup(GroupAttribute group)
            {
                if (group.FoldOut)
                {
                    var uniqueGroupID = GetGroupID(group);

                    var isToggled = SessionState.GetBool(uniqueGroupID, false);
                    GUIContent content = new GUIContent(group.GroupName);
                    isToggled = EditorGUILayout.Foldout(isToggled, content, EditorStyles.foldoutHeader);
                    SessionState.SetBool(uniqueGroupID, isToggled);
                }
                else
                {
                    EditorGUILayout.LabelField(group.GroupName, EditorStyles.boldLabel);
                }
                EditorGUI.indentLevel += 1;
            }

            void EndGroup()
            {
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.Space();
            }

            // Figure out if we're starting a new group, leaving the group, or
            // switching to a new group
            if (currentGroup == null && group != null)
            {
                // We've started a group.
                StartGroup(group);
            }
            else if (currentGroup != null && group == null)
            {
                // We've left the current group.
                EndGroup();
            }
            else if (currentGroup != null && group != null && currentGroup.Equals(group.GroupName, StringComparison.Ordinal) == false)
            {
                // We've changed group.
                EndGroup();
                StartGroup(group);
            }

            currentGroup = group?.GroupName;

            if (group?.FoldOut ?? false)
            {
                var id = GetGroupID(group);
                var isOpen = SessionState.GetBool(id, false);
                if (!isOpen)
                {
                    // We're in a group that's not open. Don't render this
                    // property.
                    return;
                }
            }


            EditorGUI.indentLevel += indentation;

            if (customPropertyRenderers.TryGetValue(property.serializedProperty.name, out var customRenderer))
            {
                // We have a custom renderer for this property - use it!
                customRenderer.Invoke(property.serializedProperty);
            }
            else
            {
                // Use the default renderer for this property
                if (label == null)
                {
                    EditorGUILayout.PropertyField(property.serializedProperty);
                }
                else
                {
                    EditorGUILayout.PropertyField(property.serializedProperty, new GUIContent(label));
                }
            }

            // Render the message boxes we've accumulated
            foreach (var box in messageBoxes)
            {
                EditorGUILayout.HelpBox(box.Message, box.Type);
            }

            EditorGUI.indentLevel -= indentation;
        }

        private void DrawPropertyField(SerializedProperty property)
        {
            if (propertyInfos.TryGetValue(property.name, out var propertyInfo))
            {
                DrawPropertyField(propertyInfo);
            }
            else
            {
                // We don't have a PropertyInfo for this property - just draw
                // the default field
                EditorGUILayout.PropertyField(property);
            }
        }

        private void OnEnable()
        {
            propertyInfos.Clear();
            customPropertyRenderers.Clear();

            // Build a dictionary of property names to SerializedProperties
            Dictionary<string, SerializedProperty> namesToProperties = new Dictionary<string, SerializedProperty>();

            // Find all properties on the target, and build a PropertyInfo
            // struct for it that will contain all of its relevant attributes.
            var propertyIterator = serializedObject.GetIterator();
            propertyIterator.NextVisible(true);
            do
            {
                var propertyInfo = new PropertyInfo(propertyIterator.Copy());
                propertyInfos.Add(propertyIterator.name, propertyInfo);

                namesToProperties[propertyIterator.name] = propertyIterator.Copy();
            } while (propertyIterator.NextVisible(false));

            // Find all methods on this object that have a CustomUI attribute,
            // and remember that that method should be called for handling the
            // relevant property.
            var methods = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<CustomUIForAttribute>();
                if (attr == null)
                {
                    // No CustomUI attribute on this method. Move on.
                    continue;
                }

                // Does the attribute reference a property that exists?
                if (propertyInfos.TryGetValue(attr.propertyName, out var prop) == false)
                {
                    // We don't have a property named attr.propertyName. Log a
                    // warning about it.
                    Debug.LogWarning($"{serializedObject.targetObject.GetType()} has no property '{attr.propertyName}' (or it is not visible)");
                    continue;
                }

                // Does the attribute reference a property that we already have
                // a renderer for?
                if (this.customPropertyRenderers.ContainsKey(attr.propertyName))
                {
                    Debug.LogWarning($"{nameof(DialogueRunnerEditor)} already has a custom renderer for {attr.propertyName}");
                    continue;
                }

                PropertyRenderer propertyRenderer = (PropertyRenderer)method.CreateDelegate(typeof(PropertyRenderer), this);
                customPropertyRenderers.Add(attr.propertyName, propertyRenderer);
            }
        }

        /// <summary>
        /// Draws the Inspector for the edited object.
        /// </summary>
        public override void OnInspectorGUI()
        {
            this.currentGroup = null;

            // Start iterating the list of properties
            var currentProperty = serializedObject.GetIterator();
            currentProperty.NextVisible(true);

            do
            {
                if (currentProperty.name == ScriptPropertyName)
                {
                    // Don't draw the 'script' property
                    continue;
                }

                DrawPropertyField(currentProperty);
            } while (currentProperty.NextVisible(false));

            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// The editor for <see cref="VoiceOverPresenter"/> objects.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VoiceOverPresenter))]
    public class VoiceOverPresenterEditor : YarnEditor { }

    /// <summary>
    /// The editor for <see cref="LinePresenter"/> objects.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LinePresenter))]
    public class LinePresenterEditor : YarnEditor { }

    /// <summary>
    /// The editor for <see cref="OptionsPresenter"/> objects.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(OptionsPresenter))]
    public class OptionsPresenterEditor : YarnEditor { }

    /// <summary>
    /// The editor for <see cref="LineAdvancer"/> objects.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LineAdvancer))]
    public class LineAdvancerEditor : YarnEditor { }

    /// <summary>
    /// The editor for <see cref="BuiltinLocalisedLineProvider"/> objects.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BuiltinLocalisedLineProvider))]
    public class BuiltinLocalisedLineProviderEditor : YarnEditor { }

    /// <summary>
    /// The editor for <see cref="DialogueRunner"/> objects.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DialogueRunner))]
    public class DialogueRunnerEditor : YarnEditor
    {
        private const string docsLabel = "Docs";
        private const string samplesLabel = "Samples";
        private const string discordLabel = "Discord";
        private const string tellUsLabel = "Tell us about your game!";
        private const string docsURL = "https://docs.yarnspinner.dev/";
        private const string discordURL = "https://discord.com/invite/yarnspinner";
        private const string tellUsURL = "https://yarnspinner.dev/tell-us";

        private const int logoMaxWidth = 240; // px, because links line is about 350px wide

        private static GUIStyle? _urlStyle = null;
        private static GUIStyle UrlStyle
        {
            get
            {
                if (_urlStyle == null)
                {
                    _urlStyle = new GUIStyle(GUI.skin.label);
                    _urlStyle.richText = true;
                }
                return _urlStyle;
            }
        }
        private static Texture2D? _yarnSpinnerLogo = null;
        private static Texture2D YarnSpinnerLogo
        {
            get
            {
                if (_yarnSpinnerLogo == null)
                {
                    string? logoPath = AssetDatabase.GUIDToAssetPath("16f8cd23bf0d0480bb8ecc39be853cda");
                    _yarnSpinnerLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath);
                }
                return _yarnSpinnerLogo;
            }
        }

        internal static void DrawYarnSpinnerHeader()
        {
            bool MakeLinkButton(string labelText)
            {
#if UNITY_6000_0_OR_NEWER
                string styledText = "<b><color=#4C8962FF><u>" + labelText + "</u></color></b>";
#else
                // Underlines aren't available in earlier versions of Unity
                string styledText = "<b><color=#4C8962FF>" + labelText + "</color></b>";
#endif
                return GUILayout.Button(styledText, UrlStyle, GUILayout.ExpandWidth(false));
            }
            void InstallSamples()
            {
                try
                {
                    // if we have the samples already installed we can just use them
                    // we don't really care HOW they got them at this point
                    // for now just open the package manager, later Mars wanted to add in a wizard here
                    if (YarnPackageImporter.IsSamplesPackageInstalled)
                    {
                        YarnPackageImporter.OpenSamplesUI();
                    }
                    else
                    {
                        // we don't have the samples installed
                        YarnPackageImporter.InstallSamples();
                    }
                }
                catch (YarnPackageImporterException ex)
                {
                    // TODO show error dialogue
                    // for now just log it
                    Debug.LogException(ex);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace(); // centre by padding from left

            // https://discussions.unity.com/t/how-to-display-an-image-logo-in-a-custom-editor/528405/9
            float imageWidth = Math.Min(EditorGUIUtility.currentViewWidth - 40, logoMaxWidth);
            float imageHeight = imageWidth * YarnSpinnerLogo.height / YarnSpinnerLogo.width;
            Rect rect = GUILayoutUtility.GetRect(imageWidth, imageHeight);
            GUI.DrawTexture(rect, YarnSpinnerLogo, ScaleMode.ScaleToFit);

            GUILayout.FlexibleSpace(); // centre by padding from right
            EditorGUILayout.EndHorizontal();

            Rect linksLine = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace(); // centre by padding from left

            if (MakeLinkButton(docsLabel)) { Application.OpenURL(docsURL); }

            // we default to assuming most of the time we don't need to change the visuals of the button
            // but if there is an installation request and it isn't yet complete
            // we set the button to be disabled
            var isActive = true;
            if (YarnPackageImporter.Status == YarnPackageImporter.SamplesPackageStatus.Installing)
            {
                isActive = false;
            }

            GUI.enabled = isActive;
            if (MakeLinkButton(samplesLabel)) { InstallSamples(); }
            GUI.enabled = true;
            if (MakeLinkButton(discordLabel)) { Application.OpenURL(discordURL); }
            if (MakeLinkButton(tellUsLabel)) { Application.OpenURL(tellUsURL); }
            GUILayout.FlexibleSpace(); // centre by padding from right

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.AddCursorRect(linksLine, MouseCursor.Link);
            EditorGUILayout.Space();
        }

        public override void OnInspectorGUI()
        {
            DrawYarnSpinnerHeader();
            base.OnInspectorGUI();
        }

        /// <summary>
        /// Draws the variable storage property in the inspector. If it's null,
        /// shows an info box.
        /// </summary>
        [CustomUIFor(nameof(DialogueRunner.variableStorage))]
        public void DrawVariableStorage(SerializedProperty variableStorageProperty)
        {
            // Draw the property. If it's null, show an info box.
            EditorGUILayout.PropertyField(variableStorageProperty);
            if (variableStorageProperty.objectReferenceValue == null)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.HelpBox($"An {ObjectNames.NicifyVariableName(nameof(InMemoryVariableStorage))} component will be added at run time.", MessageType.Info);
                EditorGUI.indentLevel -= 1;
            }
        }

        /// <summary>
        /// Draws the line provider property in the inspector.
        /// </summary>
        [CustomUIFor(nameof(DialogueRunner.lineProvider))]
        public void DrawLineProvider(SerializedProperty lineProviderProperty)
        {
            // Draw the line provider.
            EditorGUILayout.PropertyField(lineProviderProperty);

            // Check to see if the line provider is using Unity Localization. If
            // it is, offer some tools to help set it up, if needed.
            var yarnProjectProperty = lineProviderProperty.serializedObject.FindProperty(nameof(DialogueRunner.yarnProject));
            var yarnProject = yarnProjectProperty.objectReferenceValue as YarnProject;
            var yarnProjectIsUnityLoc = yarnProject != null && yarnProject.localizationType == LocalizationType.Unity;

            if (lineProviderProperty.objectReferenceValue == null)
            {
                // We don't have a line provider.
                EditorGUI.indentLevel += 1;
                if (yarnProjectIsUnityLoc)
                {
#if USE_UNITY_LOCALIZATION
                    // If this is a project using Unity localisation, we can't
                    // add a line provider at runtime because we won't know what
                    // string table it should use. In this situation, we'll show
                    // a warning and offer a quick button they can click to add
                    // one.
                    string unityLocalizedLineProvider = ObjectNames.NicifyVariableName(nameof(UnityLocalization.UnityLocalisedLineProvider));
                    EditorGUILayout.HelpBox($"This project uses Unity Localization. You will need to add a {unityLocalizedLineProvider} for it to work. Click the button below to add one, and then set it up.", MessageType.Warning);

                    GameObject gameObject = (serializedObject.targetObject as DialogueRunner)!.gameObject;

                    UnityLocalization.UnityLocalisedLineProvider existingLineProvider = gameObject.GetComponent<UnityLocalization.UnityLocalisedLineProvider>();

                    if (existingLineProvider != null)
                    {
                        // If there is an existing UnityLocalizedLineProvider,
                        // offer a button to use it.
                        if (GUILayout.Button($"Use {unityLocalizedLineProvider}"))
                        {
                            lineProviderProperty.objectReferenceValue = existingLineProvider;
                        }
                    }
                    else
                    {
                        // If there isn't an existing
                        // UnityLocalizedLineProvider, offer a button to add it.
                        if (GUILayout.Button($"Add {unityLocalizedLineProvider}"))
                        {
                            var lineProvider = gameObject.AddComponent<UnityLocalization.UnityLocalisedLineProvider>();

                            lineProviderProperty.objectReferenceValue = lineProvider;
                        }
                    }

#else
                    EditorGUILayout.HelpBox($"This project uses Unity Localization, but Unity Localization is not installed. Please install it, or change this Yarn Project to use Yarn Spinner's internal localisation system.", MessageType.Error);
#endif

                }
                else
                {
                    // Otherwise, we'll assume they're using the built-in
                    // localisation system, and we can safely create one at
                    // runtime because we know everything we need to to set that
                    // up.
                    EditorGUILayout.HelpBox($"A {ObjectNames.NicifyVariableName(nameof(BuiltinLocalisedLineProvider))} component will be added at run time.", MessageType.Info);
                }
                EditorGUI.indentLevel -= 1;
            }
            else
            {
                // We do have a line provider.

                // If it's a BuiltInLocalisationLineProvider and the project is
                // using Unity localization, that's probably a mistake, and we
                // should warn the user.
                Type lineProviderType = lineProviderProperty.objectReferenceValue.GetType();
                bool lineProviderIsUnityLineProvider = typeof(UnityLocalisedLineProvider).IsAssignableFrom(lineProviderType);

                if (yarnProjectIsUnityLoc && lineProviderIsUnityLineProvider == false)
                {
#if USE_UNITY_LOCALIZATION
                    EditorGUILayout.HelpBox($"This project uses Unity Localization, but you are using a {ObjectNames.NicifyVariableName(lineProviderType.Name)}. You should use a {ObjectNames.NicifyVariableName(nameof(UnityLocalisedLineProvider))} instead.", MessageType.Warning);
#else
                    EditorGUILayout.HelpBox($"This project uses Unity Localization, but Unity Localization is not installed. Please install it, or change this Yarn Project to use Yarn Spinner's internal localisation system.", MessageType.Error);
#endif
                }
            }
        }
    }
}
