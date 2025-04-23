using Microsoft.CodeAnalysis;


public static class Diagnostics
{
    public static readonly DiagnosticDescriptor YS1000UnknownError = new DiagnosticDescriptor(
                        "YS0000",
                        title: $"Internal unknown error",
                        messageFormat: "An internal error was encountered while processing this action: {0}",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true);
    public static readonly DiagnosticDescriptor YS1001ActionMethodsMustBePublic = new DiagnosticDescriptor(
                        "YS1001",
                        title: $"Yarn action methods must be public",
                        messageFormat: "YarnCommand and YarnFunction methods must be public. \"{0}\" is {1}.",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        description: "[YarnCommand] and [YarnFunction] attributed methods must be public so that the codegen can reference them.",
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");

    public static readonly DiagnosticDescriptor YS1002ActionMethodsMustHaveAValidName = new DiagnosticDescriptor(
                        "YS1002",
                        title: $"Yarn action methods must have a valid name",
                        messageFormat: "YarnCommand and YarnFunction methods must follow existing ID rules for Yarn. \"{0}\" is invalid.",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        description: "[YarnCommand] and [YarnFunction] attributed methods must follow Yarn ID rules so that Yarn scripts can reference them.",
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");
    public static readonly DiagnosticDescriptor YS1003CommandMethodsMustHaveAValidReturnType = new DiagnosticDescriptor(
                        "YS1003",
                        title: $"YarnCommand methods must return a valid type",
                        messageFormat: $"YarnCommand methods must return a valid type (either void, a coroutine, or a task). \"{{0}}\"'s return type is {{1}}.",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");
    public static readonly DiagnosticDescriptor YS1004FunctionMethodsMustHaveAValidReturnType = new DiagnosticDescriptor(
                        "YS1004",
                        title: $"YarnFunction methods must return a valid type",
                        messageFormat: $"YarnFunction methods must return a valid type (either bool, string, or a numeric type). \"{{0}}\"'s return type is {{1}}.",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");
    public static readonly DiagnosticDescriptor YS1005ActionMethodsMustHaveOneActionAttribute = new DiagnosticDescriptor(
                        "YS1005",
                        title: $"Yarn action methods must have a single YarnCommand or YarnAction attribute",
                        messageFormat: $"YarnCommand and YarnFunction methods must have a single attribute. \"{{0}}\" has {{1}}.",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");

    public static readonly DiagnosticDescriptor YS1006YarnFunctionsMustBeStatic = new DiagnosticDescriptor(
                        "YS1006",
                        title: $"YarnFunction methods be static",
                        messageFormat: $"YarnFunction methods are required to be static.",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");

    public static readonly DiagnosticDescriptor YS1007ActionsMustBeInPublicTypes = new DiagnosticDescriptor(
                        "YS1006",
                        title: $"Yarn action methods must be in a public type",
                        messageFormat: "Yarn actions must be in a publicly accessible type. {0}'s containing type, {1}, is {2}.",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");

}
