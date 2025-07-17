/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Yarn.Unity.ActionAnalyser;
using YarnAction = Yarn.Unity.ActionAnalyser.Action;

#nullable enable



[Generator]
public class ActionRegistrationSourceGenerator : ISourceGenerator
{
    const string YarnSpinnerUnityAssemblyName = "YarnSpinner.Unity";
    const string DebugLoggingPreprocessorSymbol = "YARN_SOURCE_GENERATION_DEBUG_LOGGING";
    const string MinimumUnityVersionPreprocessorSymbol = "UNITY_2021_2_OR_NEWER";

    public static string? GetProjectRoot(GeneratorExecutionContext context)
    {
        // We need to know if the settings are configured to not perform codegen
        // to link attributed methods. This is kinda annoying because the path
        // root of the project settings and the root path of this process are
        // *very* different. So, what we do is we use the included Compilation
        // Assembly additional file that Unity gives us. This file, if opened,
        // has the path of the Unity project, which we can then use to get the
        // settings. If any stage of this fails, then we bail out and assume
        // that codegen is desired.

        // Try and find any additional files passed to the context
        if (!context.AdditionalFiles.Any())
        {
            return null;
        }

        // One of those files is (AssemblyName).[Unity]AdditionalFile.txt, and it
        // contains the path to the project
        var relevantFiles = context.AdditionalFiles.Where(
            i => i.Path.Contains($"{context.Compilation.AssemblyName}.AdditionalFile.txt")
                || i.Path.Contains($"{context.Compilation.AssemblyName}.UnityAdditionalFile.txt")
        );

        if (!relevantFiles.Any())
        {
            return null;
        }

        var assemblyRelevantFile = relevantFiles.First();

        // The file needs to exist on disk
        if (!File.Exists(assemblyRelevantFile.Path))
        {
            return null;
        }

        try
        {
            // Attempt to read it - it should contain the path to the project directory
            var projectPath = File.ReadAllText(assemblyRelevantFile.Path);
            if (Directory.Exists(projectPath))
            {
                // If this directory exists, we're done
                return projectPath;
            }
            else
            {
                return null;
            }
        }
        catch (IOException)
        {
            // We encountered a problem while testing
            return null;
        }
    }

    public void Execute(GeneratorExecutionContext context)
    {
        using var output = GetOutput(context);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();


        output.WriteLine(DateTime.Now);


        Yarn.Unity.Editor.YarnSpinnerProjectSettings? settings = null;
        var projectPath = GetProjectRoot(context);


        if (projectPath != null)
        {
            try
            {
                var fullPath = Path.Combine(projectPath, Yarn.Unity.Editor.YarnSpinnerProjectSettings.YarnSpinnerProjectSettingsPath);
                output.WriteLine($"Attempting to read settings file at {fullPath}");

                settings = Yarn.Unity.Editor.YarnSpinnerProjectSettings.GetOrCreateSettings(projectPath, output);
                if (!settings.automaticallyLinkAttributedYarnCommandsAndFunctions)
                {
                    output.WriteLine("Skipping codegen due to settings.");
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
            output.WriteLine($"Unable to determine project location on disk. Settings values will be ignored and codegen will occur");
        }

        try
        {
            output.WriteLine("Source code generation for assembly " + context.Compilation.AssemblyName);

            if (context.AdditionalFiles.Any())
            {
                output.WriteLine($"Additional files:");
                foreach (var item in context.AdditionalFiles)
                {
                    output.WriteLine("  " + item.Path);
                }
            }

            output.WriteLine("Referenced assemblies for this compilation:");
            foreach (var referencedAssembly in context.Compilation.ReferencedAssemblyNames)
            {
                output.WriteLine(" - " + referencedAssembly.Name);
            }

            bool compilationReferencesYarnSpinner = context.Compilation.ReferencedAssemblyNames
                .Any(name => name.Name == YarnSpinnerUnityAssemblyName);

            if (compilationReferencesYarnSpinner == false)
            {
                // This compilation doesn't reference YarnSpinner.Unity. Any
                // code that we generate that references symbols in that
                // assembly won't work.
                output.WriteLine($"Assembly {context.Compilation.AssemblyName} doesn't reference {YarnSpinnerUnityAssemblyName}. Not generating any code for it.");
                return;
            }

            output.WriteLine("Preprocessor Symbols: ");
            foreach (var symbol in context.ParseOptions.PreprocessorSymbolNames)
            {
                output.WriteLine("- " + symbol);
            }

            // Don't generate source code if we're not targeting at least Unity
            // 2021.2. (Unity will not invoke this DLL as a source code
            // generator until at least this version, but other tools like
            // OmniSharp might.)
            if (!context.ParseOptions.PreprocessorSymbolNames.Contains(MinimumUnityVersionPreprocessorSymbol))
            {
                output.WriteLine($"Not generating code for assembly {context.Compilation.AssemblyName} because this assembly is not being built for Unity 2021.2 or newer");
                return;
            }


            // Don't generate source code for certain Yarn Spinner provided
            // assemblies - these always manually register any actions in them.
            var prefixesToIgnore = new List<string>()
            {
                "YarnSpinner.Unity",
                "YarnSpinner.Editor",
            };

            // But DO generate source code for the Samples assembly.
            var prefixesToKeep = new List<string>()
            {
                "YarnSpinner.Unity.Samples",
            };

            if (context.Compilation.AssemblyName == null)
            {
                output.WriteLine("Not generating registration code, because the provided AssemblyName is null");
                return;
            }

            if (prefixesToIgnore.Any(prefix => context.Compilation.AssemblyName.StartsWith(prefix)) && !prefixesToKeep.Any(prefix => context.Compilation.AssemblyName.StartsWith(prefix)))
            {
                output.WriteLine($"Not generating registration code for {context.Compilation.AssemblyName}: we've been told to exclude it, because its name begins with one of these prefixes: {string.Join(", ", prefixesToIgnore)}");
                return;

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
                actions.AddRange(Analyser.GetActions(compilation, tree, output));
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

                var diagnostics = action.Validate(compilation);
                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                    output.WriteLine($"Skipping '{action.Name}' ({action.MethodName}): {diagnostic}");
                }

                if (diagnostics.Count > 0)
                {
                    continue;
                }

                // Commands are parsed as whitespace, so spaces in the command name
                // would render the command un-callable.
                if (action.Name.Any(x => Char.IsWhiteSpace(x)))
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
            actions = actions.Where(x => !removals.Contains(x.Name)).ToList();

            output.Write($"Generating source code...");

            var source = Analyser.GenerateRegistrationFileSource(actions);

            output.WriteLine($"Done.");

            SourceText sourceText = SourceText.From(source, Encoding.UTF8);

            output.Write($"Writing generated source...");

            DumpGeneratedFile(context, source);

            output.WriteLine($"Done.");

            context.AddSource($"YarnActionRegistration-{compilation.AssemblyName}.Generated.cs", sourceText);

            if (settings != null)
            {
                if (settings.generateYSLSFile)
                {
                    output.Write($"Generating ysls...");
                    // generating the ysls

                    IEnumerable<string> commandJSON = actions.Where(a => a.Type == ActionType.Command).Select(a => a.ToJSON());
                    IEnumerable<string> functionJSON = actions.Where(a => a.Type == ActionType.Function).Select(a => a.ToJSON());

                    var ysls = "{" +
                    $@"""Commands"":[{string.Join(",", commandJSON)}]," +
                    $@"""Functions"":[{string.Join(",", functionJSON)}]" +
                    "}";

                    output.WriteLine($"Done.");

                    if (!string.IsNullOrEmpty(projectPath))
                    {
                        output.Write($"Writing generated ysls...");

                        var fullPath = Path.Combine(projectPath, Yarn.Unity.Editor.YarnSpinnerProjectSettings.YarnSpinnerGeneratedYSLSPath);
                        try
                        {
                            System.IO.File.WriteAllText(fullPath, ysls);
                            output.WriteLine($"Done.");
                        }
                        catch (Exception e)
                        {
                            output.WriteLine($"Unable to write ysls to disk: {e.Message}");
                        }
                    }
                    else
                    {
                        output.WriteLine("unable to identify project path, ysls will not be written to disk");
                    }
                }
                else
                {
                    output.WriteLine($"skipping ysls generation due to settings");
                }
            }
            else
            {
                output.WriteLine($"skipping ysls generation due to settings not being found");
            }

            stopwatch.Stop();
            output.WriteLine($"Source code generation completed in {stopwatch.Elapsed.TotalMilliseconds}ms");
            return;

        }
        catch (Exception e)
        {
            output.WriteLine($"{e}");
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

    static string GetTemporaryPath(GeneratorExecutionContext context)
    {
        string tempPath;
        var rootPath = GetProjectRoot(context);
        if (rootPath != null)
        {
            tempPath = Path.Combine(rootPath, "Logs", "Packages", "dev.yarnspinner.unity");
        }
        else
        {
            tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dev.yarnspinner.logs");
        }

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
            var tempPath = ActionRegistrationSourceGenerator.GetTemporaryPath(context);

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
            var tempPath = ActionRegistrationSourceGenerator.GetTemporaryPath(context);
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

