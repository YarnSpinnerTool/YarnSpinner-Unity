using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Yarn.Unity.Editor
{
    public static class LanguagePopup
    {
        
        static string FormatCulture(string cultureName) {
            Culture culture = Cultures.GetCulture(cultureName);
            return $"{culture.DisplayName} ({culture.Name})";
        }

        public static PopupField<string> Create(string label) {
            var allNeutralCultures = Cultures.GetCultures().Where(c => c.IsNeutralCulture);

            var defaultCulture = System.Globalization.CultureInfo.CurrentCulture;

            if (defaultCulture.IsNeutralCulture == false) {
                defaultCulture = defaultCulture.Parent;
            }

            var cultureChoices = allNeutralCultures.Select(c => c.Name).ToList();

            var popup = new PopupField<string>(label, cultureChoices, defaultCulture.Name, FormatCulture, FormatCulture);

            popup.style.flexGrow = 1;

            return popup;
        }

        public static T ReplaceElement<T>(VisualElement oldElement, T newElement) where T : VisualElement {
            oldElement.parent.Insert(oldElement.parent.IndexOf(oldElement)+1, newElement);
            oldElement.RemoveFromHierarchy();
            return newElement;
        }
    }
}
