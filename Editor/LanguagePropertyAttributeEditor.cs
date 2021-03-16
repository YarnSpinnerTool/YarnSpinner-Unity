using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Yarn.Unity
{

    [CustomPropertyDrawer(typeof(LanguageAttribute))]
    public class LanguageAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // If this property is not a string, fall back to default implementation.
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Display this property as a dropdown that lets you select a language.
            var allCultures = Cultures.GetCultures().ToList();
            var indices = Enumerable.Range(0, allCultures.Count());

            var culturesToIndicies = allCultures.Zip(indices, (culture, index) => new { culture, index }).ToDictionary(pair => pair.culture.Name, pair => pair.index);

            var value = property.stringValue;

            int currentCultureIndex;

            if (culturesToIndicies.ContainsKey(value)) {
                // The property doesn't contain a valid culture name.
                // Default it to the current locale, and also update the property so that it
                // value = System.Globalization.CultureInfo.CurrentCulture.Name;
                // property.stringValue = value;
                currentCultureIndex = culturesToIndicies[value];                
            } else {
                currentCultureIndex = -1;
            }

            
            var allCultureDisplayNames = allCultures.Select(c => c.DisplayName).Select(n => new GUIContent(n)).ToArray();

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                var selectedIndex = EditorGUI.Popup(position, label, currentCultureIndex, allCultureDisplayNames);
                if (changeCheck.changed) {
                    property.stringValue = allCultures[selectedIndex].Name;
                }
            }
        }
    }

}
