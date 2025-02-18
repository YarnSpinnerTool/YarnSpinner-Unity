/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using UnityEngine;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

namespace Yarn.Unity.Samples
{
    public enum ValueObserved
    {
        Primary, Secondary, Room, Scenario
    }

    public class ValueUpdater : MonoBehaviour
    {
        public TMP_Text textfield;
        public TheRoomVariableStorage storage;
        public ValueObserved observation;

        void Start()
        {
            UpdateLabels();
        }

        public void UpdateValue()
        {
            switch (observation)
            {
                case ValueObserved.Primary:
                {
                    storage.Primary = IncrementCharacter(storage.Primary);
                    
                    break;
                }
                case ValueObserved.Secondary:
                {
                    storage.Secondary = IncrementCharacter(storage.Secondary);

                    break;
                }

                case ValueObserved.Room:
                {
                    var allRooms = Enum.GetValues(typeof(Room));
                    for (int i = 0; i < allRooms.Length; i++)
                    {
                        var iRoom = (Room)allRooms.GetValue(i);
                        if (iRoom == storage.Room)
                        {
                            var newRoom = (Room)allRooms.GetValue((i + 1) % allRooms.Length);
                            storage.Room = newRoom;
                            break;
                        }
                    }

                    break;
                }

                case ValueObserved.Scenario:
                {
                    var allScenarios = Enum.GetValues(typeof(Scenario));
                    for (int i = 0; i < allScenarios.Length; i++)
                    {
                        var iScenario = (Scenario)allScenarios.GetValue(i);
                        if (iScenario == storage.Scenario)
                        {
                            var newScenario = (Scenario)allScenarios.GetValue((i + 1) % allScenarios.Length);
                            storage.Scenario = newScenario;
                            break;
                        }
                    }

                    break;
                }
            }

            UpdateLabels();
        }

        void UpdateLabels()
        {
            switch (observation)
            {
                case ValueObserved.Primary:
                    textfield.text = $"The Primary role is played by {storage.Primary}";
                    break;
                case ValueObserved.Secondary:
                    textfield.text = $"The Secondary role is played by {storage.Secondary}";
                    break;
                case ValueObserved.Scenario:
                    textfield.text = $"It will be a {storage.Scenario} scene";
                    break;
                case ValueObserved.Room:
                    textfield.text = $"It is set inside a {storage.Room}";
                    break;
            }
        }

        private Character IncrementCharacter(Character current)
        {
            var allCharacter = Enum.GetValues(typeof(Character));
            for (int i = 0; i < allCharacter.Length; i++)
            {
                var iCase = (Character)allCharacter.GetValue(i);
                if (iCase == current)
                {
                    var newValue = (Character)allCharacter.GetValue((i + 1) % allCharacter.Length);
                    return newValue;
                }
            }

            // we never found our character
            // we just return the current one
            // this should be impossible
            return current;
        }
    }
}