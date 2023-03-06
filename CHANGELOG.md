# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

### Added

- Methods tagged with the `YarnCommand` and `YarnFunction` attribute are now discovered at compile time, rather than at run-time. This makes game start-up significantly faster.
  - Yarn Spinner for Unity will search your source code for methods with the `YarnCommand` and `YarnFunction` attributes, and generate source code that registers these methods when the game starts up, or when you enter Play Mode in the editor.
  
    This is a change from previous versions of Yarn Spinner for Unity, which searched for commands and functions at run-time, which had performance and compatibility implications on certain platforms (notably, consoles).
    
    This search is done automatically in Unity 2021.2 and later. In earlier versions of Unity, you will need to manually tell Yarn Spinner for Unity to check your code, by opening the Window menu and choosing Yarn Spinner -> Update Yarn Commands.
- In Unity 2021.2 and later, you can now see which commands have been registered using `YarnCommand` by opening the Window menu and choosing Yarn Spinner -> Commands...
- `DialogueReference` objects can now be implicitly converted to `string`s.
- The `YarnNode` attribute can be attached to a `string` property to turn it into a drop-down menu for choosing nodes in a Yarn Project.
  ```csharp
   // A reference to a Yarn Project
  public YarnProject project;

  // A node in 'project'
  [YarnNode(nameof(project))]
  public string node1;

  // Another node in 'project'
  [YarnNode(nameof(project))]
  public string node2;
  ```

- The `YarnProject.GetHeaders` method has been added, which fetches all headers for a node.
- The `YarnProject.InitialValues` property has been added, which fetches a dictionary containing the initial values for every variable in the project.

### Changed

- Fixed a compile error in the Minimal Viable Dialogue System sample in Unity 2019.

### Removed

## [2.2.4] 2023-01-27

### Changed

- Number pluralisation rules have been updated. The rules have now use CLDR version 42.0 (previously, 36.1)
- Fixed an issue where pluralisation markup (i.e. the `plural` and `ordinal` tags) would not work correctly with country-specific locales (for example "en-AU").

## [2.2.3] 2022-11-14

### Changed

- Dependency DLLs are now aliased to prevent compilation errors with Burst.
  - In v2.2.2, Yarn Spinner's dependency DLLs were renamed to have the prefix `Yarn.` to prevent errors when two DLLs of the same name (e.g. `Google.Protobuf.dll`) are present in the same project. 
  - This fix solved the edit-time problem, but introduced a new error when the project used Unity's Burst compiler, which looks for DLL files based on their assembly name.
  - When compiling with Burst, Unity looks for the DLL file based on the name of the assembly, so when it goes searching for (for example) `Google.Protobuf`, it will _only_ look for the file `Google.Protobuf.dll`, and not the renamed file.
  - With this change, the `update_dlls.yml` build script, which pulls in the latest version of Yarn Spinner and its dependencies, now uses the [dotnet-assembly-alias](https://github.com/getsentry/dotnet-assembly-alias/) tool to rename the DLLs _and_ their assembly names.

## [2.2.2] 2022-10-31

### Added

- The `DialogueReference` class, which stores a reference to a named node in a given project (and shows a warning in the Inspector if the reference can't be found) has been added. Thanks to [@sttz](https://github.com/sttz) for the [contribution](https://github.com/YarnSpinnerTool/YarnSpinner-Unity/pull/189)! 
- Initial work on support for the Unity Localization system has been added.
  - These features are currently behind a feature flag. They are not yet considered ready for production use, and we aren't offering support for it yet.
  - To access them, add the scripting define symbol `YARN_ENABLE_EXPERIMENTAL_FEATURES`. You should only do this if you know what this involves.
  - Yarn Project importer now has initial support for Unity's Localization system.
  - A new localised line provider subclass, `UnityLocalisedLineProvider.cs` has been added.

### Changed

- Fixed interrupt token handling in `VoiceOverView` that would cause it to permanently stop a Dialogue Runner's ability to progress through dialogue lines.
- Fixed an issue where lines and options that contain invalid markup would cause an exception to be thrown, breaking dialogue. A warning message is now logged instead, and the original text of the line (with any invalid markup present) is delivered.
- Fixed a compiler error that made Yarn Spinner fail to compile on Unity 2020.1.
- The `AddCommandHandler(string name, Delegate handler)` and `AddFunction(string name, Delegate handler)` methods on `DialogueRunner` are now `public`.
  - These methods allow you to register a `Delegate` object as a command or function. 
    > **Note:**
    > We recommand that you use the pre-existing `AddCommandHandler` and `AddFunction` methods that take `System.Action` or `System.Func` parameters unless you have a very specific reason for using this, as the pre-existing methods allow the compiler to do type-checking on your command and function implementations.
- Fixed an issue that would cause compilation errors if a Unity project using Yarn Spinner also used a DLL with the same name as one of Yarn Spinner's dependencies (for example Google Protocol Buffers).
  - The dependency DLLs that come with Yarn Spinner (for example, `Antlr.Runtime`, `Google.Probuf`, and others) have been renamed to have the prefix `Yarn.`, and the assembly definition files for Yarn Spinner have been updated to use the renamed files. 
  - Huge thanks to [@Sygan](https://github.com/Sygan) for finding and describing [the fix for this problem](https://github.com/YarnSpinnerTool/YarnSpinner-Unity/issues/15#issuecomment-1036162152)!
- The `YarnProject.GetProgram()` method has been replaced with a property, `Program`.
  - `GetProgram()` still exists, but has been marked as obsolete and will be removed in a future release of Yarn Spinner.
  - `YarnSpinner.Program` has better performance, because it caches the result of de-serializing the compiled Yarn Program.

## [2.2.1] 2022-06-14

### Changed

- Fixed an issue where Yarn projects that made use of the `visited` or `visit_count` functions could produce errors when re-importing the project.

## [2.2.0] 2022-04-08

### Added

- A simple, built-in system for saving and loading Yarn variables to the built-in PlayerPrefs object has been added.
  - Call `DialogueRunner.SaveStateToPlayerPrefs` to save all variables to the `PlayerPrefs` system, and `DialogueRunner.LoadStateFromPlayerPrefs` to load from `PlayerPrefs` into the variable storage.
  - These methods do not write to file (except via `PlayerPrefs`, which handles disk writing on its own), and only work with variables (and not information like which line is currently being run.)
- Metadata for each line is exposed through a Yarn Project. Metadata generally comes as hashtags similar to `#line`. They can be used to define line-specific behavior (no particular behavior is supported by default, each user will need to define their own).
- When exporting Strings files, a Yarn Project will also export another CSV file with the line metadata (for each line with metadata).
- `LocalizedLine`s now contain a field for any metadata associated with the line.
- `YarnFunction` tagged methods now appear in the inspector window for the project, letting you view their basic info.

### Changed

- `YarnPreventPlayMode` no longer uses `WeakReference` pointing to `Unity.Object` (this is unsupported by Unity).
- `ActionManager` no longer logs every single command that it registers. (#165)
- Line view should no longer have unusual interactions around enabling and disabling different effects (#161 and #153).
- Improved the type inference system around the use of functions.
- Fixed exception when viewing a Yarn Project in the inspector that contains no declarations, in Unity 2021.2 and earlier (#168)

This has two pieces, the first is in YarnSpinner Core and adds in support for partial backwards type inference.
This means in many situations where either the l-value or r-value of an expression is known that can be used to provide a type to the other side of the equation.
Additionally now functions tagged with the `YarnFunction` attribute are sent along to the compiler so that they can be used to inform values.
The upside of this is in situations like `<<set $cats = get_cats()>>` if either `$cats` is declared or `get_cats` is tagged as a `YarnFunction` there won't be an error anymore.

### Removed

- The `SerializeAllVariablesToJSON` and `DeserializeAllVariablesFromJSON` methods have been removed.
  - If you need a simple way to save all variables, use `DialogueRunner.SaveStateToPlayerPrefs` and `DialogueRunner.LoadStateFromPlayerPrefs` instead, which save directly to Unity's PlayerPrefs system and don't require reading or writing files.
  - If your saving or loading needs are more complex, use the `VariableStorageBehaviour` class's `GetAllVariables()` and `SetAllVariables()` methods to get and set the value of all values at once, and handle the serialization and deserialization the way your game needs it.

## [2.1.0] 2022-02-17

### Dialogue View API Update

The API for creating new Dialogue Views has been updated.

> **Background:** Dialogue Views are objects that receive lines and options from the Dialogue Runner, present them to the player, receive option selections, and signal when the user should see the next piece of content. They're subclasses of the [`DialogueViewBase`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.dialogueviewbase) class. All of our built-in Dialogue Views, such as [`LineView`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.lineview) and [`OptionsListView`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.optionslistview), are examples of this.

Previously, line objects stored information about their current state, and as Dialogue Views reported that they had finished presenting or dismissing their line, all views would receive a signal that the line's state had changed, and respond to those changes by changing the way that the line was presented (such as by dismissing it when the line's state became 'Dismissed').  This meant that every Dialogue View class needed to implement fairly complex logic to handle these changes in state.

In this release, 'line state' is no longer a concept that Dialogue Views need to keep track of. Instead, Dialogue Views that present lines simply need to implement three methods:

- [`RunLine`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.dialogueviewbase/yarn.unity.dialogueviewbase.runline) is called when the Dialogue Runner wants to show a line to the player. It receives a line, as well as a completion handler to call when the line view has finished delivering the contents line.
- [`InterruptLine`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.dialogueviewbase/yarn.unity.dialogueviewbase.interruptline) is called when the Dialogue Runner wants all Dialogue Views to finish presenting their lines as fast as possible. It receives the line that's currently being presented, as well as a new completion handler to call when the presentation is finished (which this method should try and call as quickly as it can.)
- [`DismissLine`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.dialogueviewbase/yarn.unity.dialogueviewbase.dismissline) is called when all Dialogue Views have finished delivering their line (whether it was interrupted, or whether it completed normally). It receives a completion handler to call when the dismissal is complete.

The updated flow is this:

1. While running a Yarn script, the Dialogue Runner encounters a line of dialogue to show to the user.
2. It calls [`RunLine`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.dialogueviewbase/yarn.unity.dialogueviewbase.runline) on all Dialogue Views, and waits for all of them to call their completion handler to indicate that they're done presenting the line.
  * At any point, a Dialogue View can call a method that requests that the current line be interrupted. When this happens, the Dialogue Runner calls [`InterruptLine`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.dialogueviewbase/yarn.unity.dialogueviewbase.interruptline) on the Dialogue Views, and waits for them to call the new completion handler to indicate that they've finished presenting the line.
3. Once all Dialogue Views have reported that they're done, the Dialogue Runner calls [`DismissLine`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.dialogueviewbase/yarn.unity.dialogueviewbase.dismissline) on all Dialogue Views, and waits for them to call the completion handler to indicate that they're done dismissing the line.
4. The Dialogue Runner then moves on to the next piece of content.

This new flow significantly simplifies the amount of information that a Dialogue View needs to keep track of, as well as the amount of logic that a Dialogue View needs to have to manage this information.

Instead, it's a simpler model of: "when you're told to run a line, run it, and tell us when you're done. When you're told to interrupt the line, finish running it ASAP and tell us when you're done. Finally, when you're told to dismiss your line, do it and let us know when you're done."

We've also moved the user input handling code out of the built-in [`LineView`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.lineview) class, and into a new class called [`DialogueAdvanceInput`](https://docs.yarnspinner.dev/api/csharp/yarn.unity/yarn.unity.dialogueadvanceinput). This class lets you use either of the Unity Input systems to signal to a DialogueView that the user wants to advance the dialogue; by moving it out of our built-in view, it's a little easier for Dialogue View writers, who may not want to have to deal with input.

This hopefully alleviates some of the pain points in issues relating to how Dialogue Views work, like [issue #95](https://github.com/YarnSpinnerTool/YarnSpinner-Unity/issues/95).

There are no changes to how options are handled in this new API.

### Jump to Expressions

- The `<<jump>>` statement can now take an expression.

```yarn
<<set $myDestination = "Home">>
<<jump {$myDestination}>>
```

- Previously, the `jump` statement required the name of a node. With this change, it can now also take an expression that resolves to the name of a node.
- Jump expressions may be a constant string, a variable, a function call, or any other type of expression.
- These expressions must be wrapped in curly braces (`{` `}`), and must produce a string.

### Added

- Added a new component, `DialogueAdvanceInput`, which responds to user input and tells a Dialogue View to advance.
- Added `DialogueViewBase.RunLine` method.
- Added `DialogueViewBase.InterruptLine` method.
- Added `DialogueViewBase.DismissLine` method.

### Changed

- Updated to Yarn Spinner Core 2.1.0.
- Updated `DialogueRunner` to support a new, simpler API for Dialogue Views; lines no longer have a state for the Dialogue Views to respond to changes to, and instead only receive method calls that tell them what to do next.
- Fixed a bug where changing a Yarn script asset in during Play Mode would cause the string table to become empty until the next import (#154)
- Fixed a bug where Yarn functions outside the default assembly would fail to be loaded.

### Removed

- Removed `LineStatus` enum.
- Removed `DialogueViewBase.OnLineStatusChanged` method.
- Removed `DialogueViewBase.ReadyForNextLine` method.
- Removed `VoiceOverPlaybackFmod` class. (This view was largely unmaintained, and we feel that it's best for end users to customise their FMOD integration to suit their own needs.)
- Renamed `VoiceOverPlaybackUnity` to `VoiceOverView`.

## [2.0.2] 2022-01-08

### Added

- You can now specify which assemblies you want Yarn Spinner to search for `YarnCommand` and `YarnFunction` methods in.
  - By default, Yarn Spinner will search in your game's code, as well as every assembly definition in your code and your packages.
  - You can choose to make Yarn Spinner only look in specific assembly definitions, which reduces the amount of time needed to search for commands and functions.
  - To control how Yarn Spinner searches for commands and actions, turn off "Search All Assemblies" in the Inspector for a Yarn Project.
- Added a Spanish translation to the Intro sample.

### Changed

- ActionManager now only searches for commands and actions in assemblies that Yarn Projects specify. This significantly reduces startup time and memory usage.
- Improved error messages when calling methods defined via the `YarnCommand` attribute where the specified object can't be found.

## [2.0.1] 2021-12-23

### Added

- The v1 to v2 language upgrader now renames node names that have a period (`.`) in their names to use underscores (`_`) instead. Jumps and options are also updated to use these new names.

### Changed

- Fixed a crash in the compiler when producing an error message about an undeclared function.
- Fixed an error when a constant float value (such as in a `<<declare>>` statement) was parsed and the user's current locale doesn't use a period (`.`) as the decimal separator.

## [2.0.0-rc1] 2021-12-13

### Added

- Command parameters can now be grouped with double quotes. eg. `<<add-recipe "Banana Sensations">` and `<<move "My Game Object" LocationName>>` (@andiCR)

- You can now add dialogue views to dialogue runner at any time.

- The inspector for Yarn scripts now allows you to change the Project that the script belongs to. (@radiatoryang)

- Yarn script compile errors will prevent play mode.

- Default functions have been added for convenience.
  - `float random()` - returns a number between 0 and 1, inclusive (proxies Unity's default prng)
  - `float random_range(float, float)` - returns a number in a given range, inclusive (proxies Unity's default prng)
  - `int dice(int)` - returns an integer in a given range, like a dice (proxies Unity's default prng)
    - For example, `dice(6) + dice(6)` to simulate two dice, or `dice(20)` for a D20 roll.
  - `int round(float)` - rounds a number using away-from-zero rounding
  - `float round_places(float, int)` - rounds a number to n digits using away-from-zero rounding
  - `int floor(float)` - floors a number (towards negative infinity)
  - `int ceil(float)` - ceilings a number (towards positive infinity)
  - `int int(float)` - truncates the number (towards zero)
  - `int inc(float | int)` - increments to the next integer
  - `int dec(float | int)` - decrements to the previous integer
  - `int decimal(float)` - gets the decimal portion of the float

- The `YarnFunction` attribute has been added.
  - Simply add it to a static function, eg

    ```cs
    [YarnFunction] // registers function under "example"
    public static int example(int param) {
      return param + 1;
    }

    [YarnFunction("custom_name")] // registers function under "custom_name"
    public static int example2(int param) {
      return param * param;
    }
    ```

- The `YarnCommand` attribute has been improved and made more robust for most use cases.
  - You can now leave the name blank to use the method name as the registration name.

    ```cs
    [YarnCommand] // like in previous example with YarnFunction.
    void example(int steps) {
      for (int i = 0; i < steps; i++) { ... }
    }

    [YarnCommand("custom_name")] // you can still provide a custom name if you want
    void example2(int steps) {
      for (int i = steps - 1; i >= 0; i--) { ... }
    }
    ```

  - It now recognizes static functions and does not attempt to use the first parameter as an instance variable anymore.

    ```cs
    [YarnCommand] // use like so: <<example>>
    static void example() => ...;

    [YarnCommand] // still as before: <<example2 objectName>>
    void example2() => ...;
    ```

  - You can also define custom getters for better performance.

    ```cs
    [YarnStateInjector(nameof(GetBehavior))] // if this is null, the previous behavior of using GameObject.Find will still be available
    class CustomBehavior : MonoBehaviour {
      static CustomBehavior GetBehavior(string name) {
        // e.g., it may only exist under a certain transform, or you have a custom cache...
        // or it's built from a ScriptableObject...
        return ...;
      }

      [YarnCommand] // the "this" will be as returned from GetBehavior
      void example() => Debug.Log(this);

      // special variation on getting behavior
      static CustomBehavior GetBehaviorSpecial(string name) => ...;

      [YarnCommand(Injector = nameof(GetBehaviorSpecial))]
      void example_special() => Debug.Log(this);
    }
    ```

  - You can also define custom getters for Component parameters in the same vein.

    ```cs
    class CustomBehavior : MonoBehaviour {
      static Animator GetAnimator(string name) => ...;

      [YarnCommand]
      void example([YarnParameter(nameof(GetAnimator))] Animator animator) => Debug.Log(animator);
    }
    ```

  - You should continue to use manual registration if you want to make an instance function (ie where the ["target"](https://docs.microsoft.com/en-us/dotnet/api/system.delegate.target?view=netstandard-2.0) is defined) static.

- Sample scenes now have a render pipeline detector gameobject that will warn when the sample scene materials won't look correct in the current render pipeline.

- Variables declared inside Yarn scripts will now have the default values set into the variable storage.

### Changed

- Updated to support new error handling in Yarn Spinner.
  - Yarn Spinner longer reports errors by throwing an exception, and instead provides a collection of diagnostic messages in the compiler result. In Unity, Yarn Spinner will now show _all_ error messages that the compiler may produce.

- The console will no longer report an error indicating that a command is "already defined" when a subclass of a MonoBehaviour that has `YarnCommand` methods exists.

- `LocalizedLine.Text`'s setter is now public, not internal.

- `DialogueRunner` will now throw an exception if a dialogue view attempts to select an
  option on the same frame that options are run.

- `DialogueRunner.VariableStorage` can now be modified at runtime.

- Calling `DialogueRunner.StartDialogue` when the dialogue runner is already running will now result in an error being logged.

- Line Views will now only enable and disable action references if the line view is also configured to use said action.

- Yarn Project importer will now save variable declaration metadata on the first time

### Removed

- Support for Unity 2018 LTS has been dropped, and 2019 LTS (currently 2019.4.32f1) will be the minimum supported version. The support scheme for Yarn Spinner will be clarified in the [CONTRIBUTING](./CONTRIBUTING.md) docs. If you still require support for 2018, please join our [Discord](https://discord.gg/yarnspinner)!

## [2.0.0-beta5] 2021-08-17

### Added

* `InMemoryVariableStorage` now throws an exception if you attempt to get or set a variable whose name doesn't start with `$`.
* `LineView` now has an onCharacterTyped event that triggers for each character typed in the typewriter effect.

### Changed

* `OptionsListView` no longer throws a `NullPointerException` when the dialogue starts with options (instead of a line.)
* When creating a new Yarn Project file from the Assets -> Create menu, the correct icon is now used.
* Updated to use the new type system in Yarn Spinner 2.0-beta5.

### Removed

* Yarn Programs: The 'Convert Implicit Declarations' button has been temporarily removed, due to a required compatibility change related to the new type system. It will be restored before final 2.0 release.

## [2.0.0-beta4] 2021-04-01

Yarn Spinner 2.0 Beta 4 is a hotfix release for Yarn Spinner 2.0 Beta 3.

### Changed

- Fixed an issue that caused Yarn Spinner to not compile on Unity 2018 or Unity 2019.

## [2.0.0-beta3] 2021-03-27

### Added

- The dialogue runner can now be configured to log less to the console, reducing the amount of noise it generates there. (@radiatoryang)
- Warning messages and errors now appear to help users diagnose two common problems: (1) not adding a Command properly, (2) can't find a localization entry for a line (either because of broken line tag or bad connection to Localization Database) (@radiatoryang)
- Made options that have a line condition able to be presented to the player, but made unavailable.
- This change was made in order to allow games to conditionally present, but disallow, options that the player can't choose. For example, consider the following script:

```
TD-110: Let me see your identification.
-> Of course... um totally not General Kenobi and the son of Darth Vader.
    Luke: Wait, what?!
    TD-110: Promotion Time!
-> You don't need to see his identification. <<if $learnt_mind_trick is true>>
    TD-110: We don't need to see his identification.
```

- If the variable `$learnt_mind_trick` is false, a game may want to show the option but not allow the player to select it (i.e., show that this option could have been chosen if they'd learned how to do a mind trick.)
- In previous versions of Yarn Spinner, if a line condition failed, the entire option was not delivered to the game. With this change, all options are delivered, and the `OptionSet.Option.IsAvailable` variable contains `false` if the condition was not met, and `true` if it was (or was not present.)
- The `DialogueUI` component now has a "showUnavailableOptions" option that controls the display behaviour of unavailable options. If it's true, then unavailable options are presented, but not selectable; if it's false, then unavailable options are not presented at all (i.e. same as Yarn Spinner 1.0.)
- Audio for lines in a `Localization` object can now be previewed in the editor. (@radiatoryang)
- Lines can be added to a `Localization` object at runtime. They're only stored in memory, and are discarded when gameplay ends.
- Commands that take a boolean parameter now support specifying that parameter by its name, rather than requiring the string `true`.
- For example, if you have a command like this:

```csharp
  [YarnCommand("walk")]
  void WalkToPoint(string destinationName, bool wait = false) {
    // ...
  }
```

Previously, you'd need to use this in your Yarn scripts:

```
<<walk MyObject MyDestination true>>
```

With this change, you can instead say this:

```
<<walk MyObject MyDestination wait>>
```

- New icons for Yarn Spinner assets have been added.
- New dialogue views, `LineView` and `OptionListView`, have been added. These are intended to replace the previous `DialogueUI`, make use of TextMeshPro for text display, and allow for easier customisation through prefabs.
- `DialogueRunner`s will now automatically create and use an `InMemoryVariableStorage` object if one isn't provided.
- The Inspector for `DialogueRunner` has been updated, and is now easier to use.

### Changed

- Certain private methods in `DialogueUI` have changed to protected, making it easier to subclass (@radiatoryang)
- Fixed an issue where option buttons from previous option prompts could re-appear in later prompts (@radiatoryang)
- Fixed an issue where dialogue views that are not enabled were still being waited for (@radiatoryang)
- Upgrader tool now creates new files on disk, where needed (for example, .yarnproject files)
- `YarnProgram`, the asset that stores references to individual .yarn files for compilation, has been renamed to `YarnProject`. Because this change makes Unity forget any existing references to "YarnProgram" assets, **when upgrading to this version, you must set the Yarn Project field in your Dialogue Runners again.**
- `Localization`, the asset that mapped line IDs to localized data, is now automatically generated for you by the `YarnProject`.
  - You don't create them yourselves, and you no longer need to manually refresh them.
  - The `YarnProject` always creates at least one localization: the "Base" localization, which contains the original text found in your `.yarn` files.
  - You can create more localizations in the `YarnProject`'s inspector, and supply the language code to use and a `.csv` file containing replacement strings.
- Renamed the 'StartHere' demo to 'Intro', because it's not actually the first step in installing Yarn Spinner.
- Simplified the workflow for working with Addressable Assets.
  - You now import the package, enable its use on your Yarn Project, and click the Update Asset Addresses button to ensure that all assets have an address that Yarn Spinner knows about.
- The 3D, VisualNovel, and Intro examples have been updated to use the new `LineView` and `OptionsListView` components, rather than `DialogueUI`.
- `DialogueRunner.ResetDialogue` is now marked as Obsolete (it had the same effect as just calling `StartDialogue` anyway.)
- The `LineStatus` enum's values have been renamed, to better convey their purpose:
  - `Running` is now `Presenting`.
  - `Interrupted` remains the same.
  - `Delivered` is now `FinishedPresenting`.
  - `Ended` is now `Dismissed` .
- The `ResetDialogue()` method now takes an optional parameter to restart from. If none is provided, the dialogue runner attempts to restart from the start node, followed by the current node, or else throws an exception.
- `DialogueViewBase.MarkLineComplete`, the method for signalling that the user wants to interrupt or proceed to the next line, has been renamed to `ReadyForNextLine`.
- `DialogueRunner.continueNextLineOnLineFinished` has been renamed to `automaticallyContinueLines`.

### Removed

- `LocalizationDatabase`, the asset that stored references to `Localization` assets and manages per-locale line lookups, has been removed. This functionality is now handled by `YarnProject` assets. You no longer supply a localization database to a `DialogueRunner` or to a `LineProvider` - the work is handled for you.
- `AddressableAudioLineProvider` has been removed. `AudioLineProvider` now works with addressable assets, if the package is installed and your Yarn Project has been configured to use them.
- You no longer specify a list of languages available to your project in the Preferences menu or in the project settings. This is now controlled from the Yarn Project.
- The `StartDialogue()` method (with no parameters) has been removed. Instead, provide a node name to start from when calling `StartDialogue(nodeName)`.

## [2.0.0-beta2] 2021-01-14

### Added

- InMemoryVariableStorage now shows the current state of variables in the Inspector. (@radiatoryang)
- InMemoryVariableStorage now supports saving variables to file, and to PlayerPrefs. (@radiatoryang)

### Changed

- Inline expressions (for example, `One plus one is {1+1}`) are now expanded.
- Added Help URLs to various classes. (@radiatoryang)
- The Upgrader window (Window -> Yarn Spinner -> Upgrade Scripts) now uses the updated
  Yarn Spinner upgrade tools. See Yarn Spinner 2.0.0-beta2 release notes for more
  information on the upgrader.
- Fixed an issue where programs failed to import if a source script reference is invalid
- Fixed an issue where the DialogueUI would show empty lines when showCharacterName is
  false and the line has no character name

### Removed

- The `[[Destination]]` and `[[Option|Destination]]` syntax has been removed from the language.
  - This syntax was inherited from the original Yarn language, which itself inherited it from Twine.
  - We removed it for four reasons:
    - it conflated jumps and options, which are very different operations, with too-similar syntax;
    - the Option-destination syntax for declaring options involved the management of non-obvious state (that is, if an option statement was inside an `if` branch that was never executed, it was not presented, and the runtime needed to keep track of that);
    - it was not obvious that options accumulated and were only presented at the end of the node;
    - finally, shortcut options provide a cleaner way to present the same behaviour.
  - We have added a `<<jump Destination>>` command, which replaces the `[[Destination]]` jump syntax.
  - No change to the bytecode is made here; these changes only affect the compiler.
  - Instead of using ``[[Option|Destination]]`` syntax, use shortcut options instead. For example:

```
// Before
Kim: You want a bagel?
[[Yes, please!|GiveBagel]]
[[No, thanks!|DontWantBagel]]

// After
Kim: You want a bagel?
-> Yes, please!
  <<jump GiveBagel>>
-> No, thanks!
  <<jump DontWantBagel>>
```

- InMemoryVariableStorage no longer manages 'default' variables (this concept has moved to the Yarn Program.) (@radiatoryang)

## [2.0.0-beta1] 2020-10-19

### Added

- Added a 3D speech bubble sample, for dynamically positioning a speech bubble above a game character. (@radiatoryang)
- Added a phone chat sample. (@radiatoryang)
- Added a visual novel template. (@radiatoryang)
- Added support for voice overs. (@Schroedingers-Cat)
- Added a new API for presenting and managing lines of dialogue.
- Added a new API for working with localizations.
- Added an option to DialogueUI to allow hiding character names.
- The Yarn Spinner Window (Window -> Yarn Spinner) now shows the current version of Yarn Spinner.

### Changed

- Individual `.yarn` scripts are now combined into a single 'Yarn Program', which is what you provide to your `DialogueRunner`. You no longer add multiple `.yarn` files to a DialogueRunner. To create a new Yarn Program, open the Asset menu, and choose Create -> Yarn Spinner -> Yarn Program. You can also create a new Yarn Program by selecting a Yarn Script, and clicking Create New Yarn Program.
- Version 2 of the Yarn language requires variables to be declared. You can declare them in your .yarn scripts, or you can declare them in the Inspector for your Yarn Program.
  - Variables must always have a defined type, and aren't allowed to change type. This means, for example, that you can't store a string inside a variable that was declared as a number.
  - Variables also have a default value. As a result, variables are never allowed to be `null`.
  - Variable declarations can be in any part of a Yarn script. As long as they're somewhere in the file, they'll be used.
  - Variable declarations don't have to be in the same file as where they're used. If the Yarn Program contains a script that has a variable declaration, other scripts in that Program can use the variable.
  - To declare a variable on a Yarn Program, select it, and click the `+` button to create the new variable.
  - To declare a variable in a script, use the following syntax:

```
<<declare $variable_name = "hello">> // declares a string
<<declare $variable_name = 123>> // declares a number
<<declare $variable_name = true>> // declares a boolean
```

- Nicer error messages when the localized text for a line of dialogue can't be found.
- DialogueUI is now a subclass of DialogueViewBase.
- Moved Yarn Spinner classes into the `Yarn.Unity` namespace, or one of its children, depending on its purpose.
- Dialogue.AddFunction now uses functions that can take multiple parameters. You no longer use a single `Yarn.Value[]` parameter; you can now have up to 5, which can be strings, integers, floats, doubles, bools, or `Yarn.Value`s.
- Commands registered via the `YarnCommand` attribute can now take parameter types besides strings. Parameters can also be optional. Additionally, these methods are now cached, and are faster to call.

### Removed
- Commands registered via the `YarnCommand` attribute can no longer accept a `params` array of parameters. If your command takes a variable number of parameters, use optional parameters instead.

## [1.2.7]

### Changed

- Backported check for Experimental status of AssetImporter (promoted in 2020.2)

## [1.2.6]

Note: Versions 1.2.1 through 1.2.5 are identical to v1.2.0; they were version number bumps while we were diagnosing an issue in OpenUPM.

### Changed

- Fixed compiler issues in Unity 2019.3 and later by adding an explicit reference to YarnSpinner.dll in YarnSpinnerTests.asmdef

## [1.2.0] 2020-05-04

This is the first release of v1.2.0 of Yarn Spinner for Unity as a separate project. Previously, this project's source code was part of the Yarn Spinner repository. Version 1.2.0 of Yarn Spinner contains an identical release to this.

### Added

- Yarn scripts now appear with Yarn Spinner icon. (@Schroedingers-Cat)
- Documentation is updated to reflect the current version number (also to mention 2018.4 LTS as supported)
- Added a button in the Inspector for `.yarn` files in Yarn Spinner for Unity, which updates localised `.csv` files when the `.yarn` file changes. (@stalhandske, https://github.com/YarnSpinnerTool/YarnSpinner/issues/227)
- Added handlers for when nodes begin executing (in addition to the existing handlers for when nodes complete.) (@arendhil, https://github.com/YarnSpinnerTool/YarnSpinner/issues/222)
- `OptionSet.Option` now includes the name of the node that an option will jump to if selected.
- Added unit tests for Yarn Spinner for Unity (@Schroedingers-Cat)
- Yarn Spinner for Unity: Added a menu item for creating new Yarn scripts (Assets -> Create -> Yarn Script)
- Added Nuget package definitions for [YarnSpinner](http://nuget.org/packages/YarnSpinner/) and [YarnSpinner.Compiler](http://nuget.org/packages/YarnSpinner.Compiler/).

### Changed

- Fixed a crash in the compiler when parsing single-character commands (e.g. `<<p>>`) (https://github.com/YarnSpinnerTool/YarnSpinner/issues/231)
- Parse errors no longer show debugging information in non-debug builds.
