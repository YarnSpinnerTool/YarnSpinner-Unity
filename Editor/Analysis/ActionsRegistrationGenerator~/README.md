# Actions Registration Generator for Yarn Spinner

This folder contains the source code for the [source generator](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) that produces registration code for Yarn Spinner actions (for example, `YarnCommand` and `YarnAction`).

This folder ends with a tilde `~` to make Unity not aware of it. To build the source code generator, install the .NET SDK and use `dotnet-build`. The built DLL will be placed at the following path:

```
(path to Yarn Spinner)/SourceGenerator/YarnSpinner.Unity.SourceCodeGenerator.dll
```