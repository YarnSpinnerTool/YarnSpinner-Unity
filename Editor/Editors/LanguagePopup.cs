/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Yarn.Unity.Editor
{
    public static class LanguagePopup
    {

        static string FormatCulture(string cultureName)
        {
            Culture culture = Cultures.GetCulture(cultureName);
            return $"{culture.DisplayName} ({culture.Name})";
        }

        public static PopupField<string> Create(string label, bool onlyNeutralCultures = false)
        {
            var allCultures = Cultures.GetCultures().Where(c => !onlyNeutralCultures || c.IsNeutralCulture);

            var defaultCulture = System.Globalization.CultureInfo.CurrentCulture;

            if (onlyNeutralCultures && defaultCulture.IsNeutralCulture == false)
            {
                defaultCulture = defaultCulture.Parent;
            }

            var cultureChoices = allCultures.Select(c => c.Name).ToList();

            var popup = new PopupField<string>(label, cultureChoices, defaultCulture.Name, FormatCulture, FormatCulture);

            popup.style.flexGrow = 1;

            return popup;
        }

        public static T ReplaceElement<T>(VisualElement oldElement, T newElement) where T : VisualElement
        {
            oldElement.parent.Insert(oldElement.parent.IndexOf(oldElement) + 1, newElement);
            oldElement.RemoveFromHierarchy();
            return newElement;
        }
    }
}
