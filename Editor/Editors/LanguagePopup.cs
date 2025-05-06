/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
#nullable enable

namespace Yarn.Unity.Editor
{
    public class LanguageField : BaseField<string>
    {
        PopupField<string?> m_Popup;
        TextField m_TextField;

        Dictionary<string, Culture> KnownCultures = new Dictionary<string, Culture>();
        CultureInfo? DefaultCulture;

        public LanguageField() : this(null, false) { }


        public LanguageField(string? label, bool onlyNeutralCultures = false) : base(label, new Label() { })
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.flexGrow = 1;
            Add(container);
            // This is the input element instantiated for the base constructor.

            KnownCultures = Cultures.GetCultures().Where(c => !onlyNeutralCultures || c.IsNeutralCulture).ToDictionary(kv => kv.Name);

            DefaultCulture = System.Globalization.CultureInfo.CurrentCulture;

            if (onlyNeutralCultures && DefaultCulture.IsNeutralCulture == false)
            {
                DefaultCulture = DefaultCulture.Parent;
            }

            var cultureChoices = KnownCultures.Keys.Prepend(null).ToList();

            static string FormatCulture(string? cultureName)
            {
                if (cultureName == null)
                {
                    return "Custom";
                }
                else
                {
                    return Cultures.TryGetCulture(cultureName, out var culture)
                        ? $"{culture.DisplayName} ({culture.Name})"
                        : cultureName;
                }
            }

            m_Popup = new PopupField<string?>(null, cultureChoices, DefaultCulture.Name, FormatCulture, FormatCulture);

            m_Popup.style.flexGrow = 1;

            m_Popup.RegisterValueChangedCallback(OnPopupValueChanged);

            container.Add(m_Popup);

            m_TextField = new TextField();
            m_TextField.style.flexGrow = 1;
            m_TextField.RegisterValueChangedCallback(OnTextFieldValueChanged);
            container.Add(m_TextField);

            m_Popup.style.marginRight = 0;
            m_TextField.style.marginRight = 0;

            this.style.flexGrow = 1;

            //m_Input.RegisterValueChangedCallback(OnValueChanged);
            //m_Input2.RegisterValueChangedCallback(OnValueChanged);
        }

        public override string value
        {
            get => base.value; set
            {
                Debug.Log($"Language value changed from {base.value} to {value}");
                base.value = value;
            }
        }

        void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            this.value = evt.newValue;
        }

        void OnPopupValueChanged(ChangeEvent<string?> evt)
        {
            if (evt.newValue != null)
            {
                this.value = evt.newValue;
                m_TextField.style.display = DisplayStyle.None;
            }
            else
            {
                // The popup has changed to the 'custom' value; show the text field
                m_TextField.style.display = DisplayStyle.Flex;
            }
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            m_TextField.value = newValue;

            if (m_TextField.focusController?.focusedElement == m_TextField)
            {
                // The text field has focus; do not change its visibility
            }
            else
            {
                m_TextField.style.display = (!KnownCultures.ContainsKey(newValue) || m_Popup.value == null) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (KnownCultures.ContainsKey(newValue))
            {
                m_Popup.SetValueWithoutNotify(newValue);
            }
            else
            {
                m_Popup.SetValueWithoutNotify(null);
            }

            base.SetValueWithoutNotify(newValue);
        }
    }

    public static class LanguagePopup
    {
        // TODO: Remove, not needed in Unity 2022
        public static T ReplaceElement<T>(VisualElement oldElement, T newElement) where T : VisualElement
        {
            oldElement.parent.Insert(oldElement.parent.IndexOf(oldElement) + 1, newElement);
            oldElement.RemoveFromHierarchy();
            return newElement;
        }
    }
}
