using System;

namespace Yarn.Unity.Attributes
{

#nullable enable

    public abstract class YarnEditorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class IndentAttribute : YarnEditorAttribute
    {
        public int indentLevel = 1;
        public IndentAttribute(int indentLevel)
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
        public bool invert;

        public string? condition;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ShowIfAttribute : VisibilityAttribute
    {
        public ShowIfAttribute(string condition)
        {
            this.invert = false;
            this.condition = condition;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class HideIfAttribute : VisibilityAttribute
    {
        public HideIfAttribute(string condition)
        {
            this.invert = true;
            this.condition = condition;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GroupAttribute : YarnEditorAttribute {
        public string groupName;
        public bool foldOut;

        public GroupAttribute(string groupName, bool foldOut = false)
        {
            this.groupName = groupName;
            this.foldOut = foldOut;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LabelAttribute : YarnEditorAttribute {
        public string label;

        public LabelAttribute(string groupName)
        {
            this.label = groupName;
        }
    }
}
