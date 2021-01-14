# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v2.0.0-beta2] 2021-01-14

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

## [v2.0.0-beta1] 2020-10-19

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

## [v1.2.6]

Note: Versions 1.2.1 through 1.2.5 are identical to v1.2.0; they were version number bumps while we were diagnosing an issue in OpenUPM.

### Changed

- Fixed compiler issues in Unity 2019.3 and later by adding an explicit reference to YarnSpinner.dll in YarnSpinnerTests.asmdef

## [v1.2.0] 2020-05-04

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