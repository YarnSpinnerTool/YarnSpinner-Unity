/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yarn.Unity.Attributes;

namespace Yarn.Unity.Editor
{

    [CustomPropertyDrawer(typeof(LanguageAttribute))]
    public class LanguageAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (var scope = new EditorGUI.PropertyScope(position, label, property))
            {
                // If this property is not a string, show an error label. (We
                // can't call EditorGUI.PropertyField, because that would cause
                // an infinite recursion - Unity would invoke this property
                // drawer again.)
                if (property.propertyType != SerializedPropertyType.String)
                {
                    EditorGUI.HelpBox(position, $"{property.name} is not a string.", MessageType.Error);
                    return;
                }

                // Display this property as a dropdown that lets you select a
                // language.
                var allCultures = Cultures.GetCultures().ToList();
                var indices = Enumerable.Range(0, allCultures.Count());

                var culturesToIndicies = allCultures.Zip(indices, (culture, index) => new { culture, index }).ToDictionary(pair => pair.culture.Name, pair => pair.index);

                var value = property.stringValue;

                int currentCultureIndex;

                if (culturesToIndicies.ContainsKey(value))
                {
                    currentCultureIndex = culturesToIndicies[value];
                }
                else
                {
                    // The property doesn't contain a valid culture name. Show
                    // an 'empty' value, which will be replaced when the user
                    // selects a valid value from the dropdown.                
                    currentCultureIndex = -1;
                }

                var allCultureDisplayNames = allCultures.Select(c => (c.DisplayName + $":({c.Name})")).Select(n => new GUIContent(n)).ToArray();

                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    var selectedIndex = EditorGUI.Popup(position, label, currentCultureIndex, allCultureDisplayNames);
                    if (changeCheck.changed)
                    {
                        property.stringValue = allCultures[selectedIndex].Name;
                    }
                }
            }
        }
    }

}
