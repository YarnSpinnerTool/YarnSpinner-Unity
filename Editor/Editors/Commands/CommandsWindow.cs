/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Yarn.Unity;

    internal class CommandsCollection : IActionRegistration
    {
        public List<Actions.CommandRegistration> commandRegistrations = new List<Actions.CommandRegistration>();

        public List<(string Name, Delegate Function)> functionRegistrations = new List<(string Name, Delegate Function)>();

        public IEnumerable<CommandsWindow.IListItem> GetListItems()
        {
            foreach (var registrationMethod in Actions.ActionRegistrationMethods)
            {
                registrationMethod.Invoke(this, RegistrationType.Compilation);
            }

            yield return new CommandsWindow.HeaderListItem { DisplayName = "Commands" };

            foreach (var command in commandRegistrations)
            {
                yield return new CommandsWindow.CommandListItem { Command = command };
            }

            // Add a fake 'stop' command to the list, so that it appears in the
            // window
            System.Action fakeStop = () => { };
            yield return new CommandsWindow.CommandListItem
            {
                Command = new Actions.CommandRegistration("stop", fakeStop)
            };
        }

        public void AddCommandHandler(string commandName, Delegate handler)
        {
            commandRegistrations.Add(new Actions.CommandRegistration(commandName, handler));

        }

        public void AddCommandHandler(string commandName, MethodInfo methodInfo)
        {
            commandRegistrations.Add(new Actions.CommandRegistration(commandName, methodInfo));
        }

        public void AddFunction(string name, Delegate implementation) => functionRegistrations.Add((name, implementation));

        public void RemoveCommandHandler(string commandName)
        {
            // No-op
        }

        public void RemoveFunction(string name)
        {
            // No-op
        }
    }

    public class CommandsWindow : EditorWindow
    {
        public interface IListItem { string DisplayName { get; } }

        public class HeaderListItem : IListItem
        {
            public string DisplayName { get; set; }
        }

        public class CommandListItem : IListItem
        {
            internal Actions.CommandRegistration Command;
            public string DisplayName => Command.Name;
        }

        [SerializeField] private VisualTreeAsset uxml;
        [SerializeField] private VisualTreeAsset listItemUXML;
        [SerializeField] private StyleSheet stylesheet;

        private List<IListItem> listItems;
        private List<IListItem> filteredListItems;

        [MenuItem("Window/Yarn Spinner/Commands...")]
        static void Summon()
        {
            var window = GetWindow<CommandsWindow>("Yarn Commands");
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

            listItems = new List<IListItem>(commandsCollection.GetListItems().OrderBy(item => item.DisplayName));

            UpdateFilter(listView, searchField.value);

            // Set ListView.makeItem to initialize each entry in the list.
            listView.makeItem = () =>
            {
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
                    element.Q<Label>("Subtitle").text = "<<" + command.Command.UsageString + ">>";
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
                        return command.Command.Name.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) != -1;
                    }
                    return true;
                }).ToList();
            }
            listView.itemsSource = filteredListItems;
        }
    }
}
