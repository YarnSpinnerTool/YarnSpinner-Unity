# Minimal Viable Dialogue System Sample

Unlike most samples this one is less about showing off the flexibility of the components provided as-is and more about giving an example of the approach you can take when you want more control over Yarn Spinner.
The core of this sample is the minimal dialogue runner that interfaces with the Yarn Project and dialogue instead of using the pre-made one.
This also required making new line and option list view equivalents and some other pieces as needed.
This document is to show a breakdown of the scene because of these differences, as well as give guidance to the approach taken to making this scene and the components within.

## Controlling the dialogue

The dialogue is started by pressing the Space key which will being the `Start` node in the dialogue.
To advance each line press the Continue button.
To select an option use the mouse.

## The scene

The scene is a standard 3D unity scene with an unpacked and modified version of the Dialogue System prefab.
We took the approach that because UI is hard and we already had a working UI prefab it made more sense to modify that than to essentially rebuild the exact same thing.
The existing Dialogue Runner component was removed and replaced with the new Minimal Dialogue Runner component.
This uses some code from the full runner but for the most part is completely built from scratch for this same.

Next the line view component was also deleted and replaced with a Minimal Line View.
This is mostly copy-pasted from the existing line view but with some features removed and rewritten to instead of relying on callbacks to directly call `Continue`.
As part of this the continue button action was hooked up to the Minimal Line View.

Finally the Options List View component was deleted and replaced with a Minimal Options View.
Like the line view this is mostly copy-pasted and reworked version of the existing OptionsListView component.
This does still use the existing OptionView and has to make a wrapper around `SetSelectedOption` look like a callback so that the OptionView didn't need to be changed.

Overall the modified Dialogue Prefab structure is as follows with bolded items are modified from the prefab:
- **Dialogue System**
    - Canvas
        - **Line View**
            - Background
            - Character Name
            - Divider
            - Text
            - **Continue Button**
        - **Options List View**
            - Background
            - Last Line
    - Event System

## Dialogue Support Component

This component exists solely to capture and log any events that aren't relevant to presenting lines or options and to have a very basic means of triggering the dialogue to start, by pressing the Space key after the scene has loaded.
In a larger game this would likely be multiple different components. 

## Minimal Runner Differences

Unlike the full runner which uses callbacks into the views for control and presentation the minimal runner uses Unity Events and public methods.
The Unity Events signal important moments, such as a line needs presentation, or the dialogue has finished, and the public methods on the runner `Continue` and `SetSelectedOption` allow you to control the flow of the dialogue.
The line and option events go to their respective views, all other events go to the support component where they are logged, in more complex examples more could be done with them.

Unlike the full runner the minimal runner does no checking on the state of views and will advance dialogue as soon as told to do so.
This means it may well send new lines and options and commands to other pieces of the game that aren't ready, this is the risk of having full control.

One of the largest differences this runner has over the full runner is around commands.
When using the full runner the dispatching of commands is handled for you, here the support component just logs the command and continues the dialogue.
