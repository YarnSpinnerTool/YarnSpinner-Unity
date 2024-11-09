using System;

namespace Yarn.Unity
{

#nullable enable

    public abstract class YarnEditorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class IndentAttribute : YarnEditorAttribute
    {
        public int indentLevel = 1;
        public IndentAttribute(int indentLevel = 1)
        {
            if (this.indentLevel < 0)
            {
                indentLevel = 0;
            }
            this.indentLevel = indentLevel;
        }
    }

    public abstract class VisibilityAttribute : YarnEditorAttribute
    {
        public enum AttributeMode
        {
            BooleanCondition,
            EnumEquality,
        }
        public AttributeMode Mode { get; protected set; }

        public bool Invert { get; protected set; }

        public string? Condition { get; protected set; }

        public int EnumValue { get; protected set; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ShowIfAttribute : VisibilityAttribute
    {

        public ShowIfAttribute(string condition)
        {
            this.Invert = false;
            this.Condition = condition;
            this.Mode = AttributeMode.BooleanCondition;
            this.EnumValue = default;
        }

        public ShowIfAttribute(string condition, object value)
        {
            this.Invert = false;
            this.Condition = condition;
            this.Mode = AttributeMode.EnumEquality;
            this.EnumValue = (int)value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class HideIfAttribute : VisibilityAttribute
    {
        public HideIfAttribute(string condition)
        {
            this.Invert = true;
            this.Condition = condition;
        }

        public HideIfAttribute(string condition, object value)
        {
            this.Invert = true;
            this.Condition = condition;
            this.Mode = AttributeMode.EnumEquality;
            this.EnumValue = (int)value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GroupAttribute : YarnEditorAttribute
    {
        public string groupName;
        public bool foldOut;

        public GroupAttribute(string groupName, bool foldOut = false)
        {
            this.groupName = groupName;
            this.foldOut = foldOut;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LabelAttribute : YarnEditorAttribute
    {
        public string Label { get; }

        public LabelAttribute(string groupName)
        {
            this.Label = groupName;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MustNotBeNullAttribute : YarnEditorAttribute
    {
        public string? Label { get; }
        public MustNotBeNullAttribute(string? label = null)
        {
            this.Label = label;
        }
    }
        }
    }
}
