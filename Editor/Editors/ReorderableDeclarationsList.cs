using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Yarn.Unity.Editor
{
    public class ReorderableDeclarationsList
    {
        private struct Problem
        {
            public string text;
            public int index;
        }

        public bool IsSearching => string.IsNullOrEmpty(filterString) == false;
        public bool HasProblems => problems.Count > 0;

        private ReorderableList _list;
        private string filterString;

        private List<int> filteredIndices = new List<int>();
        private List<Problem> problems = new List<Problem>();

        private SerializedObject serializedObject;
        private SerializedProperty serializedProperty;

        private Dictionary<int, float> _lineHeights = new Dictionary<int, float>();

        public ReorderableDeclarationsList(SerializedObject serializedObject, SerializedProperty property)
        {
            this.serializedObject = serializedObject;
            serializedProperty = property;

            _list = new UnityEditorInternal.ReorderableList(serializedObject, property, false, true, true, true)
            {
                drawHeaderCallback = (rect) => DrawListHeader(rect, "Declarations"),
                drawElementCallback = (rect, index, isActive, isFocused) => DrawListElement(rect, index, isActive, isFocused, useSearch: true),
                elementHeightCallback = (index) => GetElementHeight(index, useSearch: true),
                onAddCallback = OnAdd,
                onCanAddCallback = OnCanAdd,
                onCanRemoveCallback = OnCanRemove,
            };

            UpdateProblems();
        }

        private void OnAdd(ReorderableList list)
        {
            serializedProperty.InsertArrayElementAtIndex(serializedProperty.arraySize);
            var entry = serializedProperty.GetArrayElementAtIndex(serializedProperty.arraySize - 1);

            // Clear necessary properties to something useful
            var nameProp = entry.FindPropertyRelative("name");
            var typeProp = entry.FindPropertyRelative("typeName");
            var defaultValueStringProp = entry.FindPropertyRelative("defaultValueString");
            var sourceYarnAssetProp = entry.FindPropertyRelative("sourceYarnAsset");
            var descriptionProp = entry.FindPropertyRelative("description");

            nameProp.stringValue = "$variable";
            typeProp.enumValueIndex = YarnProjectImporter.SerializedDeclaration.BuiltInTypesList.IndexOf(Yarn.BuiltinTypes.String);
            defaultValueStringProp.stringValue = string.Empty;
            sourceYarnAssetProp.objectReferenceValue = null;
            descriptionProp.stringValue = string.Empty;

        }

        private void UpdateProblems()
        {
            problems.Clear();

            var declarations = Cast<SerializedProperty>(serializedProperty.GetEnumerator()).ToList();

            // Find all variables with duplicate names
            var names = declarations.Select((p, index) => new { name = p.FindPropertyRelative("name").stringValue, index });

            var duplicateNames = names.GroupBy(s => s.name)
                                    .Where(g => g.Count() > 1)
                                    .Select(g => g.Key)
                                    .ToList();

            problems.AddRange(duplicateNames.Select(name => new Problem
            {
                text = $"Duplicate variable name {name}",
                index = names.First(n => n.name == name).index
            }));

            var invalidVariableNames = names.Where(decl => decl.name.Equals(string.Empty) == false && !decl.name.StartsWith("$"));

            problems.AddRange(invalidVariableNames.Select(decl => new Problem { text = $"Variable name '{decl.name}' must begin with a $", index = decl.index }));

            var emptyVariableNames = names.Where(name => string.IsNullOrEmpty(name.name));

            problems.AddRange(emptyVariableNames.Select(decl => new Problem { text = $"Variable name must not be empty", index = decl.index }));

        }

        IEnumerable<T> Cast<T>(IEnumerator iterator)
        {
            while (iterator.MoveNext())
            {
                yield return (T)iterator.Current;
            }
        }

        private bool OnCanRemove(ReorderableList list)
        {
            var isFromScript = serializedProperty.GetArrayElementAtIndex(list.index).FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null;
            return IsSearching == false && !isFromScript;
        }

        private bool OnCanAdd(ReorderableList list)
        {
            return IsSearching == false;
        }

        private bool ShouldShowElement(int index)
        {
            // If we're not searching then all indices are shown
            if (IsSearching == false)
            {
                return true;
            }

            // Otherwise, we show this element if its index is in the
            // filtered list
            return filteredIndices.Contains(index);
        }

        private float GetElementHeight(int index, bool useSearch)
        {
#if !UNITY_2022_1_OR_NEWER
            // Before Unity 2022.1, this callback gets called with 
            // index 0 even if the list is empty
            if (serializedProperty.arraySize == 0)
            {
                return 0;
            }
#endif

            if (useSearch == false || ShouldShowElement(index))
            {
                if (!_lineHeights.ContainsKey(index))
                {
                    var item = serializedProperty.GetArrayElementAtIndex(index);
                    _lineHeights[index] = DeclarationPropertyDrawer.GetPropertyHeightImpl(item, null);
                }
                return _lineHeights[index];
            }
            else
            {
                return 0;
            }
        }

        private void DrawListHeader(Rect rect, string label)
        {
            GUI.Label(rect, label);
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused, bool useSearch)
        {
            if (useSearch == false || ShouldShowElement(index))
            {
                var item = serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, item);
            }
        }

        public void DrawLayout()
        {
            _lineHeights.Clear();
            //serializedObject.Update();
            EditorGUILayout.Space();

            foreach (var problem in problems)
            {
                if (problem.index != -1)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.HelpBox(problem.text, MessageType.Error);
                        if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                        {
                            _list.index = problem.index;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(problem.text, MessageType.Error);
                }
                EditorGUILayout.Space();
            }

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                filterString = EditorGUILayout.TextField("Search", filterString);

                if (changeCheck.changed)
                {
                    UpdateSearch(filterString);
                }
            }


            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                _list.DoLayoutList();

                if (changeCheck.changed)
                {
                    UpdateProblems();
                }
            }


            if (IsSearching && filteredIndices.Count == 0)
            {
                EditorGUILayout.LabelField("No items to show.");
            }
        }

        private void UpdateSearch(string filterString)
        {
            filteredIndices.Clear();

            if (string.IsNullOrEmpty(filterString) == false)
            {

                var count = serializedProperty.arraySize;
                for (int i = 0; i < count; i++)
                {


                    var item = serializedProperty.GetArrayElementAtIndex(i);

                    var nameProperty = item.FindPropertyRelative("name");
                    if (nameProperty.stringValue?.Contains(filterString) ?? false)
                    {
                        filteredIndices.Add(i);
                    }
                }
            }
        }
    }
}
