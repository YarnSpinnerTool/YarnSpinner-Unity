# 3D Speech Bubble

This sample demonstrates the basics of how to position speech bubbles dynamically above characters in a 3D (or 2D) game world.
It does this through a `DialogueViewBase` subclass.

This sample highlights:

- custom dialogue views
- extracting character names from lines of dialogue

To get started play the scene, there are no controls and the dialogue will start itself.
Click on the continue button on the lines to advance dialogue.

## YarnCharacter.cs

This class is responsible for associating a game object with a specific character in the Yarn Dialogue.
It also has some code `messageBubbleOffset` for offsetting the dialogue, this is often useful to avoid any awkward overlap between a character's lines of dialogue and their on-screen model.

## YarnCharacterView.cs

This is the `DialogueViewBase` subclass for the scene.
It manages the different `YarnCharacter`s and has code for calculating the screen position based on a characters world position.