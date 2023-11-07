# Pausing the Typewrier

This sample demonstrates how to use the pausing markup (`[pause /]`) to pause in the middle of line.
This sample also shows using the `onPauseStarted` and `onPauseEnded` events to respond to the dialogue pause by changing a sprite.

This sample highlights:

- using markup in lines
- using attributes inside of markup
- self-closing markup properties
- pausing the built in effects
- responding to events

To start the sample play the scene and the dialogue will begin by itself.
Click the continue button to advance lines.

## PauseResponder.cs

This class is a basic demonstration of using the pause start and end events the line view provides to swap a sprite.
In this case there are only two sprites `thinking` and `talking` but the idea would work for multiple.
When a line is paused the paused started event is fired and the face sprite is swapped to the thinking face, and when the pause ends it goes back to the default.