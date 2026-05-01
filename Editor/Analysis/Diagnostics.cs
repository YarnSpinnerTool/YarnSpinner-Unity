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
                        defaultSeverity: DiagnosticSeverity.Error,
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
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");
    public static readonly DiagnosticDescriptor YS1004FunctionMethodsMustHaveAValidReturnType = new DiagnosticDescriptor(
                        "YS1004",
                        title: $"YarnFunction methods must return a valid type",
                        messageFormat: $"YarnFunction methods must return a valid type (either bool, string, or a numeric type). \"{{0}}\"'s return type is {{1}}.",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Error,
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
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");
    
    public static readonly DiagnosticDescriptor YS1008ActionsParamsArraysMustBeOfYarnTypes = new DiagnosticDescriptor(
                        "YS1008",
                        title: "Params arrays must be of a Yarn compatible type",
                        messageFormat: "Params arrays must be of a Yarn compatible type, but {0} is of type \"{1}\"",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions");
    
    public static readonly DiagnosticDescriptor YS1009ActionsEnumAttributedParameterIsOfIncompatibleType = new DiagnosticDescriptor(
                        "YS1009",
                        title: "Yarn Enum attributed parameters must be of a Yarn compatible type",
                        messageFormat: "Yarn Enum attributed parameters must be of a Yarn compatible type, but {0} is of type \"{1}\"",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions");
    
    public static readonly DiagnosticDescriptor YS1010ActionsNodeAttributedParameterIsOfIncompatibleType = new DiagnosticDescriptor(
                        "YS1010",
                        title: "Yarn Node attributed parameters must be a string",
                        messageFormat: "Yarn Node attributed parameters must be a string, but {0} is of type \"{1}\"",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions");
    
    public static readonly DiagnosticDescriptor YS1011ActionsParameterIsAnIncompatibleType = new DiagnosticDescriptor(
                        "YS1011",
                        title: "Yarn action parameters must be of a Yarn compatible type",
                        messageFormat: "Yarn action parameters must be of a Yarn compatible type, but {0} is of type \"{1}\"",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions");
    
    public static readonly DiagnosticDescriptor YS1012ActionIsALambda = new DiagnosticDescriptor(
                        "YS1012",
                        title: "Yarn actions can be lambdas but this generally isn't recommended",
                        messageFormat: "Yarn actions can be lambdas but this generally isn't recommended. Lambda based actions cannot be unregistered and are more difficult to debug",
                        category: "Yarn Spinner",
                        defaultSeverity: DiagnosticSeverity.Info,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions");

}
