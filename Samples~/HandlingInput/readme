# User Input and Yarn

This sample demonstrates how to use TMP input fields to get user input into Yarn variables.
This is just one potential approach, there are multiple ways to achieve this.

This sample highlights:

- Blocking commands
- String interpolation
- Adding commands
- Setting Yarn variables from outside of Yarn scripts

To get started play the scene and the dialogue will start itself.
Click on the continue button on the lines to advance dialogue.
After a while the dialogue will progress to a point where you need to enter your name, type your name into the field and press Enter.

## InputHandler.cs

This class registers a new blocking command `input` that takes in a single parameter, the name of the variable.
In this sample it is used in the dialogue to set the `$name` variable, so `<<input name>>`.

This is used in a coroutine to fade up the canvas group `inputGroup`, which contains a Text Mesh Pro Input Field.
The coroutine then waits until the On Edit End event is fired.
The `OnEditEnd` event on the input field then stores the value of the input.

This allow the coroutine to stop waiting and set the value of the `$name` variable in Yarn.
Once this is done it then fades down the canvas group and finishes, allowing the dialogue runner to continue.
