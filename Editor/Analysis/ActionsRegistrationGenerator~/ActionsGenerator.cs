/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using Yarn.Unity.ActionAnalyser;
using YarnAction = Yarn.Unity.ActionAnalyser.Action;
using System.IO;

[Generator]
public class ActionRegistrationSourceGenerator : ISourceGenerator
{
    const string YarnSpinnerUnityAssemblyName = "YarnSpinner.Unity";
    const string DebugLoggingPreprocessorSymbol = "YARN_SOURCE_GENERATION_DEBUG_LOGGING";

    public void Execute(GeneratorExecutionContext context)
    {
        var output = GetOutput(context);
        output.WriteLine(DateTime.Now);

        // we need to know if the settings are configured to not perform codegen to link attributed methods
        // this is kinda annoying because the path root of the project settings and the root path of this process are *very* different
        // so what we do is we use the included Compilation Assembly additional file that Unity gives us.
        // This file if opened has the path of the Unity project, which we can then use to get the settings
        // if any stage of this fails then we bail out and assume that codegen is desired
        if (context.AdditionalFiles.Any())
        {
            var relevants = context.AdditionalFiles.Where(i => i.Path.Contains($"{context.Compilation.AssemblyName}.AdditionalFile.txt"));
            if (relevants.Any())
            {
                var arsgacsaf = relevants.First();
                if (File.Exists(arsgacsaf.Path))
                {
                    try
                    {
                        var projectPath = File.ReadAllText(arsgacsaf.Path);
                        var fullPath = Path.Combine(projectPath, Yarn.Unity.Editor.YarnSpinnerProjectSettings.YarnSpinnerProjectSettingsPath);
                        output.WriteLine($"Attempting to read settings file at {fullPath}");

                        if (!Yarn.Unity.Editor.YarnSpinnerProjectSettings.GetOrCreateSettings(projectPath, output).automaticallyLinkAttributedYarnCommandsAndFunctions)
                        {
                            output.WriteLine("Skipping codegen due to settings.");
                            output.Dispose();
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        output.WriteLine($"Unable to determine Yarn settings, settings values will be ignored and codegen will occur: {e.Message}");
                    }
                }
                else
                {
                    output.WriteLine($"The project settings path metadata file does not exist at: {arsgacsaf.Path}. Settings values will be ignored and codegen will occur");
                }
            }
            else
            {
                output.WriteLine("Unable to determine Yarn settings path, no file containing the project path metadata was included. Settings values will be ignored and codegen will occur.");
            }
        }
        else
        {
            output.WriteLine("Unable to determine Yarn settings path as no additional files were included. Settings values will be ignored and codegen will occur.");
        }
        
        try
        {
            output.WriteLine("Source code generation for assembly " + context.Compilation.AssemblyName);

            if (context.AdditionalFiles.Any()) {
                output.WriteLine($"Additional files:");
                foreach (var item in context.AdditionalFiles) {
                    output.WriteLine("  " + item.Path);
                }
            }

            output.WriteLine("Referenced assemblies for this compilation:");
            foreach (var referencedAssembly in context.Compilation.ReferencedAssemblyNames) {
                output.WriteLine(" - " + referencedAssembly.Name);
            }

            bool compilationReferencesYarnSpinner = context.Compilation.ReferencedAssemblyNames
                .Any(name => name.Name == YarnSpinnerUnityAssemblyName);

            if (compilationReferencesYarnSpinner == false) {
                // This compilation doesn't reference YarnSpinner.Unity. Any
                // code that we generate that references symbols in that
                // assembly won't work.
                output.WriteLine($"Assembly {context.Compilation.AssemblyName} doesn't reference {YarnSpinnerUnityAssemblyName}. Not generating any code for it.");
                return;
            }

            // Don't generate source code for certain Yarn Spinner provided
            // assemblies - these always manually register any actions in them.
            var prefixesToIgnore = new List<string>()
            {
                "YarnSpinner.Unity",
                "YarnSpinner.Editor",
            };

            foreach (var prefix in prefixesToIgnore)
            {
                if (context.Compilation.AssemblyName.StartsWith(prefix))
                {
                    output.WriteLine($"Not generating registration code for {context.Compilation.AssemblyName}: we've been told to exclude it, because its name begins with one of these prefixes: {string.Join(", ", prefixesToIgnore)}");
                    return;
                }
            }

            if (!(context.Compilation is CSharpCompilation compilation))
            {
                // This is not a C# compilation, so we can't do analysis.
                output.WriteLine($"Stopping code generation because compilation is not a {nameof(CSharpCompilation)}.");
                return;
            }

            var actions = new List<YarnAction>();
            foreach (var tree in compilation.SyntaxTrees)
            {
                actions.AddRange(Analyser.GetActions(compilation, tree, output).Where(a => a.DeclarationType == DeclarationType.Attribute));
            }

            if (actions.Any() == false)
            {
                output.WriteLine($"Didn't find any Yarn Actions in {context.Compilation.AssemblyName}. Not generating any source code for it.");
                return;
            }

            HashSet<string> removals = new HashSet<string>();
            // validating and logging all the actions
            foreach (var action in actions)
            {
                if (action == null)
                {
                    output.WriteLine($"Action is null??");
                    continue;
                }

                // if an action isn't public we will log a warning
                // and then later we will also skip it
                if (action.MethodSymbol.DeclaredAccessibility != Accessibility.Public)
                {
                    var descriptor = new DiagnosticDescriptor(
                        "YS1001",
                        $"Yarn {action.Type} methods must be public",
                        "YarnCommand and YarnFunction methods must be public. \"{0}\" is {1}.",
                        "Yarn Spinner",
                        DiagnosticSeverity.Warning,
                        true,
                        "[YarnCommand] and [YarnFunction] attributed methods must be public so that the codegen can reference them.",
                        "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");
                    context.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(
                        descriptor,
                        action.Declaration?.GetLocation(),
                        action.MethodIdentifierName, action.MethodSymbol.DeclaredAccessibility
                    ));
                    output.WriteLine($"Action {action.Name} will be skipped due to it's declared accessibility {action.MethodSymbol.DeclaredAccessibility}");
                    removals.Add(action.Name);
                    continue;
                }

                // this is not a full validation of the naming rules of commands
                // but is good enough to catch the most common situations, whitespace and periods
                if (action.Name.Contains(".") || action.Name.Any(x => Char.IsWhiteSpace(x)))
                {
                    var descriptor = new DiagnosticDescriptor(
                        "YS1002",
                        $"Yarn {action.Type} methods must have a valid name",
                        "YarnCommand and YarnFunction methods follow existing ID rules for Yarn. \"{0}\" is invalid.",
                        "Yarn Spinner",
                        DiagnosticSeverity.Warning,
                        true,
                        "[YarnCommand] and [YarnFunction] attributed methods must follow Yarn ID rules so that Yarn scripts can reference them.",
                        "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");
                    context.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(
                        descriptor,
                        action.Declaration?.GetLocation(),
                        action.Name
                    ));
                    output.WriteLine($"Action {action.MethodIdentifierName} will be skipped due to it's name {action.Name}");
                    removals.Add(action.Name);
                    continue;
                }

                output.WriteLine($"Action {action.Name}: {action.SourceFileName}:{action.Declaration?.GetLocation()?.GetLineSpan().StartLinePosition.Line} ({action.Type})");
            }

            // removing any actions that failed validation
            actions.Where(x => !removals.Contains(x.Name)).ToList();

            output.Write($"Generating source code...");

            var source = Analyser.GenerateRegistrationFileSource(actions);

            output.WriteLine($"Done.");

            SourceText sourceText = SourceText.From(source, Encoding.UTF8);

            output.Write($"Writing generated source...");

            DumpGeneratedFile(context, source);

            output.WriteLine($"Done.");

            context.AddSource($"YarnActionRegistration-{compilation.AssemblyName}.Generated.cs", sourceText);


            return;

        }
        catch (Exception e)
        {
            output.WriteLine($"{e}");
        }
        finally
        {
            output.Dispose();
        }
    }

    private MethodDeclarationSyntax GenerateLoggingMethod(string methodName, string sourceExpression, string prefix)
    {
        return SyntaxFactory.MethodDeclaration(
    SyntaxFactory.PredefinedType(
        SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
    SyntaxFactory.Identifier(methodName))
.WithModifiers(
    SyntaxFactory.TokenList(
        new[]{
            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
            SyntaxFactory.Token(SyntaxKind.StaticKeyword)}))
.WithBody(
    SyntaxFactory.Block(
        SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("IEnumerable"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.StringKeyword))))))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier("source"))
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ParseExpression(sourceExpression)))))),
        SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName(
                    SyntaxFactory.Identifier(
                        SyntaxFactory.TriviaList(),
                        SyntaxKind.VarKeyword,
                        "var",
                        "var",
                        SyntaxFactory.TriviaList())))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier("prefix"))
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(prefix))))))),
        SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Debug"),
                    SyntaxFactory.IdentifierName("Log")
                )
            )
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                        SyntaxFactory.Argument(
                            SyntaxFactory.InterpolatedStringExpression(
                                SyntaxFactory.Token(SyntaxKind.InterpolatedVerbatimStringStartToken)
                            )
                            .WithContents(
                                SyntaxFactory.List<InterpolatedStringContentSyntax>(
                                    new InterpolatedStringContentSyntax[]{
                                        SyntaxFactory.Interpolation(
                                            SyntaxFactory.IdentifierName("prefix")
                                        ),
                                        SyntaxFactory.InterpolatedStringText()
                                        .WithTextToken(
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(),
                                                SyntaxKind.InterpolatedStringTextToken,
                                                " ",
                                                " ",
                                                SyntaxFactory.TriviaList()
                                            )
                                        ),
                                        SyntaxFactory.Interpolation(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.PredefinedType(
                                                        SyntaxFactory.Token(SyntaxKind.StringKeyword)
                                                    ),
                                                    SyntaxFactory.IdentifierName("Join")
                                                )
                                            )
                                            .WithArgumentList(
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                        new SyntaxNodeOrToken[]{
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.LiteralExpression(
                                                                    SyntaxKind.CharacterLiteralExpression,
                                                                    SyntaxFactory.Literal(';')
                                                                )
                                                            ),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("source")
                                                            )
                                                        }
                                                    )
                                                )
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            )
        )
    )
)
.NormalizeWhitespace();
    }

    public static MethodDeclarationSyntax GenerateSingleLogMethod(string methodName, string text, string prefix)
    {
        return SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(
                SyntaxFactory.Token(SyntaxKind.VoidKeyword)
            ),
            SyntaxFactory.Identifier(methodName)
        )
        .WithModifiers(
            SyntaxFactory.TokenList(
                new[]{
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                }
            )
        )
        .WithBody(
            SyntaxFactory.Block(
                SyntaxFactory.SingletonList<StatementSyntax>(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("Debug"),
                                SyntaxFactory.IdentifierName("Log")
                            )
                        )
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.InterpolatedStringExpression(
                                            SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken)
                                        )
                                        .WithContents(
                                            SyntaxFactory.List<InterpolatedStringContentSyntax>(
                                                new InterpolatedStringContentSyntax[]{
                                                    SyntaxFactory.Interpolation(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal(prefix)
                                                        )
                                                    ),
                                                    SyntaxFactory.InterpolatedStringText()
                                                    .WithTextToken(
                                                        SyntaxFactory.Token(
                                                            SyntaxFactory.TriviaList(),
                                                            SyntaxKind.InterpolatedStringTextToken,
                                                            " ",
                                                            " ",
                                                            SyntaxFactory.TriviaList()
                                                        )
                                                    ),
                                                    SyntaxFactory.Interpolation(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal(text)
                                                        )
                                                    )
                                                }
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            )
        )
        .NormalizeWhitespace();
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassDeclarationSyntaxReceiver());
    }

    static string TemporaryPath()
    {
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dev.yarnspinner.logs");

        // we need to make the logs folder, but this can potentially fail
        // if it does fail then we will just chuck the logs inside the tmp folder
        try
        {
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
        }
        catch
        {
            tempPath = System.IO.Path.GetTempPath();
        }
        return tempPath;
    }

    public Yarn.Unity.ILogger GetOutput(GeneratorExecutionContext context)
    {
        if (GetShouldLogToFile(context))
        {
            var tempPath = ActionRegistrationSourceGenerator.TemporaryPath();

            var path = System.IO.Path.Combine(tempPath, $"{nameof(ActionRegistrationSourceGenerator)}-{context.Compilation.AssemblyName}.txt");
            var outFile = System.IO.File.Open(path, System.IO.FileMode.Create);

            return new Yarn.Unity.FileLogger(new System.IO.StreamWriter(outFile));
        }
        else
        {
            return new Yarn.Unity.NullLogger();
        }
    }

    private static bool GetShouldLogToFile(GeneratorExecutionContext context)
    {
        return context.ParseOptions.PreprocessorSymbolNames.Contains(DebugLoggingPreprocessorSymbol);
    }

    public void DumpGeneratedFile(GeneratorExecutionContext context, string text)
    {
        if (GetShouldLogToFile(context))
        {
            var tempPath = ActionRegistrationSourceGenerator.TemporaryPath();
            var path = System.IO.Path.Combine(tempPath, $"{nameof(ActionRegistrationSourceGenerator)}-{context.Compilation.AssemblyName}.cs");
            System.IO.File.WriteAllText(path, text);
        }
    }
}

internal class ClassDeclarationSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> Classes { get; private set; } = new List<ClassDeclarationSyntax>();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // Business logic to decide what we're interested in goes here
        if (syntaxNode is ClassDeclarationSyntax cds)
        {
            Classes.Add(cds);
        }
    }
}
