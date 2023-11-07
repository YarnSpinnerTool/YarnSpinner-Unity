# Space

This sample demonstrates a basic 2D side-scrolling game with conversations featuring localisation and voiceovers.

This sample highlights:

- localisation
- voice over
- custom commands
- branching dialogue
- associating characters to nodes

To get started play the scene.
You can walk about using `A` and `D` keys and can start conversations with characters by standing next to them and pressing `Space bar`.
To advance dialogue press the continue button and to select an option click on it.

## NPC.cs

This class associates a a node with a character and is used by the `PlayerCharacter` class to determine what node of dialogue to run.

## PlayerCharacter.cs

This class is responsible both for movement and for beginning dialogue.
When the space bar is pressed it does a check of all `NPC`s within the `interactionRadius` and uses this information to tell the dialogue runner what node to run.

## SpriteSwitcher.cs

This class provides a new command `<<setsprite spritename>>` which is used in the dialogue to change the face of the computer NPC at different points.