# Visual Novel

This sample demonstrates how you can use Yarn commands to build out much of the functionality of a visual novel.
There is a lot of custom classes and sprite manipulation in this sample.

This sample highlights:

- associating characters and sprites
- custom commands
- custom dialogue views
- animation in dialogue

To get started play the scene and the dialogue will start itself.
Click on the dialogue text to advance the story and select options from the options buttons.

## VNManager.cs

This class is responsible for the commands to manipulate sprites, and also manages the sprites themselves.
It does this through 15 custom sprite related commands.
The VNManager is also responsible for holding the list of actors in the scene vai a custom data class `VNActor`.