/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Editor
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class CommandsCollection
    {
        private List<Yarn.Compiler.Definitions>? yslses;

        private List<Yarn.Compiler.Definitions> loadDefintions()
        {
            var definitions = new List<Yarn.Compiler.Definitions>();

            // getting all text assets in the project
            // this I think gets those both in assets and packages
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }
                // ok is it a ysls.json file?
                if (!path.EndsWith(".ysls.json"))
                {
                    continue;
                }

                var source = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                var ysls = new Yarn.Compiler.Definitions(source);
                definitions.Add(ysls);
            }

            // now to check the project settings folder to see if it has any ysls files also
            var settingPaths = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ProjectSettings", "Packages", "dev.yarnspinner");
            try
            {
                var yslsPaths = Directory.EnumerateFiles(settingPaths, "*.ysls.json");
                foreach (var yslsPath in yslsPaths)
                {
                    if (string.IsNullOrWhiteSpace(yslsPath))
                    {
                        continue;
                    }
                    var source = File.ReadAllBytes(yslsPath);
                    var ysls = new Yarn.Compiler.Definitions(source);
                    definitions.Add(ysls);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            return definitions;
        }

        public IEnumerable<CommandsWindow.IListItem> GetCommandItems()
        {
            if (yslses == null)
            {
                yslses = loadDefintions();
            }

            var allCommands = yslses.SelectMany(y => y.commands);

            foreach (var command in allCommands)
            {
                yield return new CommandsWindow.CommandListItem(command);
            }

            // Add a fake 'stop' command to the list, so that it appears in the window
            yield return new CommandsWindow.CommandListItem("stop", "<<stop>>");
        }
        public IEnumerable<CommandsWindow.IListItem> GetFunctionItems()
        {
            if (yslses == null)
            {
                yslses = loadDefintions();
            }

            var allFunctions = yslses.SelectMany(y => y.functions);
            foreach (var function in allFunctions)
            {
                yield return new CommandsWindow.CommandListItem(function);
            }

            // how do I want to handle the built in functions?
            // most are just gonna be noise (so like Number.Add) but ones like visited are good to know right?
        }
    }

    public class CommandsWindow : EditorWindow
    {
        public interface IListItem { string DisplayName { get; } }

        public class HeaderListItem : IListItem
        {
            public string DisplayName { get; set; }

            public HeaderListItem(string displayName)
            {
                DisplayName = displayName;
            }
        }

        public class CommandListItem : IListItem
        {
            internal CommandListItem(Yarn.Shared.Action command)
            {
                DisplayName = command.Name;
                UsageString = command.Usage;
            }

            internal CommandListItem(Compiler.Declaration function)
            {
                DisplayName = function.Name;
                if (function.Type is not FunctionType fType)
                {
                    throw new System.ArgumentException($"attempted to create a function declaration when we don't have a function, we have a {function.Type}");
                }
                UsageString = $"{function.Name}({string.Join(',', fType.Parameters.Select(p => p.Name))})";
            }

            internal CommandListItem(string display, string usage)
            {
                DisplayName = display;
                UsageString = usage;
            }

            public string DisplayName { get; private set; }
            public string UsageString;
        }

        [SerializeField] private VisualTreeAsset? uxml;
        [SerializeField] private VisualTreeAsset? listItemUXML;
        [SerializeField] private StyleSheet? stylesheet;

        private List<IListItem> listItems = new();
        private List<IListItem> filteredListItems = new();

        [MenuItem("Window/Yarn Spinner/Commands and Functions...")]
        static void Summon()
        {
            var window = GetWindow<CommandsWindow>("Yarn Actions");
            window.Show();
        }

        void CreateGUI()
        {
            if (uxml == null)
            {
                Debug.LogWarning($"{GetType()}'s {nameof(uxml)} is null");
                return;
            }
            uxml.CloneTree(rootVisualElement);
            var listView = rootVisualElement.Q<ListView>();

            var searchField = rootVisualElement.Q<UnityEditor.UIElements.ToolbarSearchField>();

            searchField.RegisterValueChangedCallback(evt =>
            {
                UpdateFilter(listView, searchField.value);
            });

            var commandsCollection = new CommandsCollection();

            listItems = new List<IListItem>
            {
                new CommandsWindow.HeaderListItem("Commands")
            };
            listItems.AddRange(commandsCollection.GetCommandItems().OrderBy(item => item.DisplayName));
            listItems.Add(new CommandsWindow.HeaderListItem("Functions"));
            listItems.AddRange(commandsCollection.GetFunctionItems().OrderBy(item => item.DisplayName));

            UpdateFilter(listView, searchField.value);

            // Set ListView.makeItem to initialize each entry in the list.
            listView.makeItem = () =>
            {
                if (listItemUXML == null)
                {
                    throw new InvalidOperationException($"Can't create new list item: {nameof(listItemUXML)} is null");
                }

                var result = listItemUXML.CloneTree();
                result.styleSheets.Add(stylesheet);
                result.AddToClassList("commandListItem");
                return result;
            };

            // Set ListView.bindItem to bind an initialized entry to a data item.
            listView.bindItem = (VisualElement element, int index) =>
            {
                var listItem = filteredListItems[index];
                element.Q<Label>("Title").text = listItem.DisplayName;

                element.RemoveFromClassList("header");
                element.RemoveFromClassList("command");
                if (listItem is HeaderListItem)
                {
                    element.AddToClassList("header");
                }
                else if (listItem is CommandListItem command)
                {
                    element.AddToClassList("command");
                    element.Q<Label>("Subtitle").text = command.UsageString;
                }
            };
        }

        private void UpdateFilter(ListView listView, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                filteredListItems = listItems;
            }
            else
            {
                filteredListItems = listItems.Where(item =>
                {
                    if (item is HeaderListItem)
                    {
                        return true;
                    }
                    if (item is CommandListItem command)
                    {
                        return command.DisplayName.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) != -1;
                    }
                    return true;
                }).ToList();
            }
            listView.itemsSource = filteredListItems;
        }
    }
}
