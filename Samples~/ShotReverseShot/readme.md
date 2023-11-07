# Shot Reverse Shot

This sample demonstrates how to use Cinemachine to create a custom dialogue view that can do a basic shot-reverse-shot conversation camera.
Note this sample *requires* the Cinemachine package to be installed.

This sample highlights:

- custom dialogue views
- using cinemachine in conjunction with Yarn Spinner
- associating game objects and characters

The dialogue will not start automatically.
To start the dialogue play the scene and press `'e'`.
Click the continue button to advance the dialogue and click on options to select one.

## Virtual Cameras

The scene has three Cinemachine virtual cameras, one that looks at the scene on the whole and is used when not in conversation.
And each of the characters have their own that looks at their face.
These cameras are toggled on and off by the `ShotReverseShotCamerDialogueView`

## ShotReverseShotCamerDialogueView.cs

This is a custom dialogue view subclass that manages the virtual cameras.
It has an array of `CharacterCamera` which is a basic struct that associates a character name with a virtual camera.

When a line comes in the view will extract the character name from the line and then using the `CharacterCamera`s find the right virtual camera for that character.
This will then be enabled, and the others disabled, which will make cinemachine switch the shot.