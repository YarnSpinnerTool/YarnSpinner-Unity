using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Yarn.Unity.ActionAnalyser
{

    public class Analyser
    {
        const string toolName = "YarnActionAnalyzer";
        const string toolVersion = "1.0.0.0";
        const string registrationMethodName = "RegisterActions";
        const string targetParameterName = "target";
        const string initialisationMethodName = "AddRegisterFunction";

        /// <summary>
        /// The name of a scripting define symbol that, if set, indicates that Yarn actions specific to unit tests should be generated.
        /// </summary>
        public const string GenerateTestActionRegistrationSymbol = "YARN_GENERATE_TEST_ACTION_REGISTRATIONS";

        public Analyser(string sourcePath)
        {
            this.SourcePath = sourcePath;
        }

        public IEnumerable<string> SourceFiles => GetSourceFiles(SourcePath);
        public string SourcePath { get; set; }

        public IEnumerable<Action> GetActions()
        {
            var trees = SourceFiles
                .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
                .ToList();

            var systemAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            if (string.IsNullOrEmpty(systemAssemblyPath))
            {
                throw new AnalyserException("Unable to find an assembly that defines System.Object");
            }

            var references = new List<MetadataReference> {
                MetadataReference.CreateFromFile(
                    typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(
                    GetTypeByName("Yarn.Unity.YarnCommandAttribute").Assembly.Location),
                MetadataReference.CreateFromFile(
                    GetTypeByName("UnityEngine.MonoBehaviour").Assembly.Location),
                MetadataReference.CreateFromFile(
                    typeof(System.Collections.IEnumerator).Assembly.Location),
                MetadataReference.CreateFromFile(
                    Path.Combine(systemAssemblyPath, "System.dll")),
                MetadataReference.CreateFromFile(
                    typeof(System.Attribute).Assembly.Location),
            };

            var compilation = CSharpCompilation.Create("YarnActionAnalysis")
                .AddReferences(references)
                .AddSyntaxTrees(trees);

            var output = new List<Action>();

            var diagnostics = compilation.GetDiagnostics();

            try
            {
                foreach (var tree in trees)
                {
                    output.AddRange(GetActions(compilation, tree));
                }
            }
            catch (System.Exception e)
            {
                throw new AnalyserException(e.Message, e, diagnostics);
            }

            return output;
        }

        public static string GenerateRegistrationFileSource(IEnumerable<Action> actions, string @namespace = "Yarn.Unity.Generated", string className = "ActionRegistration")
        {
            var namespaceDecl = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(@namespace));

            var classDeclaration = SyntaxFactory.ClassDeclaration(className);
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            classDeclaration = classDeclaration.AddAttributeLists(GeneratedCodeAttributeList);

            MethodDeclarationSyntax registrationMethod = GenerateRegistrationMethod(actions);
            MethodDeclarationSyntax initializationMethod = GenerateInitialisationMethod();

            classDeclaration = classDeclaration.AddMembers(
                initializationMethod,
                registrationMethod
            );


            namespaceDecl = namespaceDecl.AddMembers(classDeclaration);

            return namespaceDecl.NormalizeWhitespace().ToFullString();
        }

        private static MethodDeclarationSyntax GenerateInitialisationMethod()
        {
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)
                ),
                SyntaxFactory.Identifier(initialisationMethodName)
            )
            .WithAttributeLists(
                SyntaxFactory.List(
                    new AttributeListSyntax[]{
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Attribute(
                                    SyntaxFactory.QualifiedName(
                                        SyntaxFactory.AliasQualifiedName(
                                            SyntaxFactory.IdentifierName(
                                                SyntaxFactory.Token(SyntaxKind.GlobalKeyword)
                                            ),
                                            SyntaxFactory.IdentifierName("UnityEditor")
                                        ),
                                        SyntaxFactory.IdentifierName("InitializeOnLoadMethod")
                                    )
                                )
                            )
                        )
                        .WithOpenBracketToken(
                            SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Trivia(
                                        SyntaxFactory.IfDirectiveTrivia(
                                            SyntaxFactory.IdentifierName("UNITY_EDITOR"),
                                            true,
                                            true,
                                            true
                                        )
                                    )
                                ),
                                SyntaxKind.OpenBracketToken,
                                SyntaxFactory.TriviaList()
                            )
                        ),
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Attribute(
                                    SyntaxFactory.QualifiedName(
                                        SyntaxFactory.AliasQualifiedName(
                                            SyntaxFactory.IdentifierName(
                                                SyntaxFactory.Token(SyntaxKind.GlobalKeyword)
                                            ),
                                            SyntaxFactory.IdentifierName("UnityEngine")
                                        ),
                                        SyntaxFactory.IdentifierName("RuntimeInitializeOnLoadMethod")
                                    )
                                )
                                .WithArgumentList(
                                    SyntaxFactory.AttributeArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.AttributeArgument(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.AliasQualifiedName(
                                                            SyntaxFactory.IdentifierName(
                                                                SyntaxFactory.Token(SyntaxKind.GlobalKeyword)
                                                            ),
                                                            SyntaxFactory.IdentifierName("UnityEngine")
                                                        ),
                                                        SyntaxFactory.IdentifierName("RuntimeInitializeLoadType")
                                                    ),
                                                    SyntaxFactory.IdentifierName("BeforeSceneLoad")
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                        .WithOpenBracketToken(
                            SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Trivia(
                                        SyntaxFactory.EndIfDirectiveTrivia(
                                            true
                                        )
                                    )
                                ),
                                SyntaxKind.OpenBracketToken,
                                SyntaxFactory.TriviaList()
                            )
                        )
                    }
                )
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
                                    SyntaxFactory.IdentifierName("Actions"),
                                    SyntaxFactory.IdentifierName("AddRegistrationMethod")
                                )
                            )
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.IdentifierName(registrationMethodName)
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

        public static MethodDeclarationSyntax GenerateRegistrationMethod(IEnumerable<Action> actions)
        {
            var actionGroups = actions.GroupBy(a => a.SourceFileName);

            var registrationStatements = actionGroups.SelectMany(group =>
            {
                return group.Select((a, i) =>
                {
                    var registrationStatement = a.GetRegistrationSyntax(targetParameterName);
                    if (i == 0)
                    {
                        return registrationStatement.WithLeadingTrivia(SyntaxFactory.TriviaList(
                            SyntaxFactory.Comment($"// Actions from file:"),
                            SyntaxFactory.Comment($"// {a.SourceFileName}")
                        ));
                    }
                    else
                    {
                        return registrationStatement;
                    }
                });
            });

            var registrationMethodBody = SyntaxFactory.Block().WithStatements(SyntaxFactory.List(registrationStatements));

            var attributes = SyntaxFactory.List(new[] { GeneratedCodeAttributeList });

            var methodSyntax = SyntaxFactory.MethodDeclaration(
                attributes, // attribute list
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                ), // modifiers
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), // return type
                null, // explicit interface identifier
                SyntaxFactory.Identifier(registrationMethodName), // method name
                null, // type parameter list
                SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(targetParameterName)).WithType(SyntaxFactory.ParseTypeName("global::Yarn.Unity.IActionRegistration"))
                )), // parameters
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), // type parameter constraints
                registrationMethodBody, // body
                null, // arrow expression clause
                SyntaxFactory.Token(SyntaxKind.None) // semicolon token
                );

            return methodSyntax.NormalizeWhitespace();
        }

        private static AttributeListSyntax GeneratedCodeAttributeList
        {
            get
            {
                var toolNameArgument = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(toolName)
                    )
                );

                var toolVersionArgument = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(toolVersion)
                    )
                );

                return SyntaxFactory.AttributeList(
                    SyntaxFactory.SeparatedList(
                        new[] {
                            SyntaxFactory.Attribute(
                                SyntaxFactory.ParseName("System.CodeDom.Compiler.GeneratedCode"),
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        new[] {
                                            toolNameArgument,
                                            toolVersionArgument,
                                        }
                                    )
                                )
                            )
                        }
                    )
                );
            }
        }

        public static IEnumerable<Action> GetActions(CSharpCompilation compilation, Microsoft.CodeAnalysis.SyntaxTree tree)
        {
            var root = tree.GetCompilationUnitRoot();

            SemanticModel model = null;

            if (compilation != null) {
                model = compilation.GetSemanticModel(tree);
            }

            return GetAttributeActions(root, model).Concat(GetRuntimeDefinedActions(root, model));
        }

        private static IEnumerable<Action> GetRuntimeDefinedActions(CompilationUnitSyntax root, SemanticModel model)
        {
            var methodInvocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Select(i =>
                {
                    // Get the symbol represening the method that's being called
                    var methodSymbol = model.GetSymbolInfo(i.Expression).Symbol as IMethodSymbol;
                    return (Syntax: i, Symbol: methodSymbol);
                })
                .Where(i => i.Symbol != null).ToList();

            var dialogueRunnerCalls = methodInvocations
                .Where(info => info.Symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Yarn.Unity.DialogueRunner").ToList();

            var addCommandCalls = methodInvocations.Where(
                info => info.Symbol.Name == "AddCommandHandler"
            ).ToList();

            foreach (var call in addCommandCalls)
            {
                var methodNameSyntax = call.Syntax.ArgumentList.Arguments.ElementAtOrDefault(0);
                var targetSyntax = call.Syntax.ArgumentList.Arguments.ElementAtOrDefault(1);
                if (methodNameSyntax == null || targetSyntax == null)
                {
                    continue;
                }

                if (!(model.GetConstantValue(methodNameSyntax.Expression).Value is string name))
                {
                    // TODO: handle case of 'we couldn't figure out the constant value here'
                    continue;
                }


                if (!(model.GetSymbolInfo(targetSyntax.Expression).Symbol is IMethodSymbol targetSymbol))
                {
                    // TODO: handle case of 'we couldn't figure out target method's
                    // symbol'
                    continue;
                }

                var declaringSyntax = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

                yield return new Action
                {
                    Name = name,
                    SemanticModel = model,
                    Type = ActionType.Command,
                    MethodName = targetSymbol.Name,
                    MethodSymbol = targetSymbol,
                    Declaration = null,
                    SourceFileName = root.SyntaxTree.FilePath,
                    DeclarationType = DeclarationType.DirectRegistration,
                };
            }
        }

        private static IEnumerable<Action> GetAttributeActions(CompilationUnitSyntax root, SemanticModel model)
        {

            var methodInfos = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(decl => decl.Parent is ClassDeclarationSyntax);

            var methodsAndSymbols = methodInfos
                .Select(decl =>
                {
                    return (MethodDeclaration: decl, Symbol: model.GetDeclaredSymbol(decl));
                })
                .Where(pair => pair.Symbol != null)
                .Where(pair => pair.Symbol.DeclaredAccessibility == Accessibility.Public);

            var actionMethods = methodsAndSymbols
                .Select(pair =>
                {
                    var actionType = GetActionType(model, pair.MethodDeclaration, out var attr);

                    return (pair.MethodDeclaration, pair.Symbol, ActionType: actionType, ActionAttribute: attr);
                })
                .Where(info => info.ActionType != ActionType.NotAnAction);

            foreach (var methodInfo in actionMethods)
            {
                var attr = methodInfo.ActionAttribute;

                string actionName;

                // If the attribute has an argument list, and the first argument
                // is a string, then use that string's value as the action name.
                if (attr.ArgumentList != null
                    && attr.ArgumentList.Arguments.First().Expression is LiteralExpressionSyntax commandName
                    && commandName.Kind() == SyntaxKind.StringLiteralExpression
                    && commandName.Token.Value is string name)
                {
                    actionName = name;
                }
                else
                {
                    // Otherwise, use the method's name.
                    actionName = methodInfo.MethodDeclaration.Identifier.ToString();
                }

                var position = methodInfo.MethodDeclaration.GetLocation();
                var lineIndex = position.GetLineSpan().StartLinePosition.Line + 1;

                var container = methodInfo.Symbol.ContainingSymbol as ITypeSymbol;

                var containerName = container?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "<unknown>";

                yield return new Action
                {
                    Name = actionName,
                    Type = methodInfo.ActionType,
                    MethodName = $"{containerName}.{methodInfo.MethodDeclaration.Identifier}",
                    MethodSymbol = methodInfo.Symbol,
                    IsStatic = methodInfo.Symbol.IsStatic,
                    Declaration = methodInfo.MethodDeclaration,
                    Parameters = new List<Parameter>(GetParameters(methodInfo.Symbol)),
                    AsyncType = GetAsyncType(methodInfo.Symbol),
                    SemanticModel = model,
                    SourceFileName = root.SyntaxTree.FilePath,
                    DeclarationType = DeclarationType.Attribute,
                };
            }
        }

        /// <summary>
        /// Returns a value indicating the Unity async type for this action.
        /// </summary>
        /// <param name="symbol">The method symbol to test.</param>
        /// <returns></returns>
        private static AsyncType GetAsyncType(IMethodSymbol symbol)
        {
            var returnType = symbol.ReturnType;

            if (returnType.SpecialType == SpecialType.System_Void)
            {
                return AsyncType.Sync;
            }

            // If the method returns IEnumerator, it is a coroutine, and therefore async.
            if (returnType.SpecialType == SpecialType.System_Collections_IEnumerator)
            {
                return AsyncType.AsyncCoroutine;
            }

            // If the method returns a Coroutine, then it is potentially async
            // (because if it returns null, it's sync, and if it returns non-null,
            // it's async)
            if (returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::UnityEngine.Coroutine")
            {
                return AsyncType.MaybeAsyncCoroutine;
            }

            // If it's anything else, then this action is invalid. Return the
            // default value; other parts of the action detection process will throw
            // errors.
            return default;


        }

        private static IEnumerable<Parameter> GetParameters(IMethodSymbol symbol)
        {
            foreach (var param in symbol.Parameters)
            {
                yield return new Parameter
                {
                    Name = param.Name,
                    IsOptional = param.IsOptional,
                    Type = param.Type,
                };
            }
        }

        internal static bool IsAttributeYarnCommand(AttributeData attribute)
        {
            return GetActionType(attribute) != ActionType.NotAnAction;
        }

        internal static ActionType GetActionType(SemanticModel model, MethodDeclarationSyntax decl, out AttributeSyntax actionAttribute)
        {
            var attributes = GetAttributes(decl, model);

            var actionTypes = attributes
                .Select(attr => (Syntax: attr.Item1, Data: attr.Item2, Type: GetActionType(attr.Item2)))
                .Where(info => info.Type != ActionType.NotAnAction);

            if (actionTypes.Count() != 1)
            {
                // Not an action, because you can only have one YarnCommand or
                // YarnFunction attribute
                actionAttribute = null;
                return ActionType.NotAnAction;
            }

            var actionType = actionTypes.Single();
            actionAttribute = actionType.Syntax;
            return actionType.Type;
        }

        internal static ActionType GetActionType(AttributeData attr)
        {
            INamedTypeSymbol attributeClass = attr.AttributeClass;

            switch (attributeClass.Name)
            {
                case "YarnCommandAttribute": return ActionType.Command;
                case "YarnFunctionAttribute": return ActionType.Function;
                default: return ActionType.NotAnAction;
            }
        }

        public static IEnumerable<(AttributeSyntax, AttributeData)> GetAttributes(MethodDeclarationSyntax method, SemanticModel model)
        {
            var methodSymbol = model.GetDeclaredSymbol(method);

            var methodAttributes = methodSymbol.GetAttributes();

            foreach (var attribute in methodAttributes)
            {
                var syntax = attribute.ApplicationSyntaxReference.GetSyntax() as AttributeSyntax;
                yield return (syntax, attribute);
            }
        }

        internal static IEnumerable<string> GetSourceFiles(string sourcePath)
        {
            if (Directory.Exists(sourcePath))
            {
                return System.IO.Directory.EnumerateFiles(sourcePath, "*.cs", SearchOption.AllDirectories);
            }
            if (File.Exists(sourcePath))
            {
                return new[] { sourcePath };
            }
            throw new System.IO.FileNotFoundException("No file at the provided path was found.", sourcePath);
        }

        public static Type GetTypeByName(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var tt = assembly.GetType(name);
                if (tt != null)
                {
                    return tt;
                }
            }

            return null;
        }
    }
}
