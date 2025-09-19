/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

#nullable enable

namespace Yarn.Unity.ActionAnalyser
{

    static class EnumerableExtensions
    {
        private struct Comparer<TItem, TKey> : IEqualityComparer<TItem>
        {
            Func<TItem, TKey> KeyFunc;
            public Comparer(Func<TItem, TKey> keyFunc) => this.KeyFunc = keyFunc;

            public readonly bool Equals(TItem x, TItem y)
            {
                var xKey = KeyFunc(x);
                var yKey = KeyFunc(y);
                return (xKey == null && yKey == null) || (xKey != null && xKey.Equals(yKey));
            }

            public readonly int GetHashCode(TItem obj) => KeyFunc(obj)?.GetHashCode() ?? 0;
        }
        public static IEnumerable<TItem> DistinctBy<TItem, TKey>(this IEnumerable<TItem> enumerable, Func<TItem, TKey> key) => Enumerable.Distinct(enumerable, new Comparer<TItem, TKey>(key));
    }

    public class Analyser
    {
        const string toolName = "YarnActionAnalyzer";
        const string toolVersion = "1.0.0.0";
        const string registrationMethodName = "RegisterActions";
        const string targetParameterName = "target";
        const string registrationTypeParameterName = "registrationType";
        const string initialisationMethodName = "AddRegisterFunction";

        /// <summary>
        /// The name of a scripting define symbol that, if set, indicates that
        /// Yarn actions specific to unit tests should be generated.
        /// </summary>
        public const string GenerateTestActionRegistrationSymbol = "YARN_GENERATE_TEST_ACTION_REGISTRATIONS";

        public Analyser(string sourcePath)
        {
            this.SourcePath = sourcePath;
        }

        public IEnumerable<string> SourceFiles => GetSourceFiles(SourcePath);
        public string SourcePath { get; set; }

        public IEnumerable<Action> GetActions(IEnumerable<string>? assemblyPaths = null, bool onlyValid = false)
        {
            var trees = SourceFiles
                .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
                .ToList();

            var systemAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            if (string.IsNullOrEmpty(systemAssemblyPath))
            {
                throw new AnalyserException("Unable to find an assembly that defines System.Object");
            }

            static string GetLocationOfAssemblyWithType(string typeName)
            {
                return GetTypeByName(typeName)?.Assembly.Location ?? throw new AnalyserException($"Failed to find an assembly for type " + typeName);
            }

            var runtiem = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

            var netstandard = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "netstandard");
            var systemCore = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "System.Core");

            var references = new List<MetadataReference> {
                MetadataReference.CreateFromFile(netstandard.Location),
                MetadataReference.CreateFromFile(systemCore.Location),
                MetadataReference.CreateFromFile(
                    typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(
                    GetLocationOfAssemblyWithType("Yarn.Unity.DialogueRunner")),
                MetadataReference.CreateFromFile(
                    GetLocationOfAssemblyWithType("Yarn.Unity.YarnCommandAttribute")),
                MetadataReference.CreateFromFile(
                    GetLocationOfAssemblyWithType("UnityEngine.MonoBehaviour")),
            };

            if (assemblyPaths != null)
            {
                references.AddRange(assemblyPaths.Select(p => MetadataReference.CreateFromFile(p)));
            }

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
            catch (Exception e)
            {
                throw new AnalyserException(e.Message, e, diagnostics);
            }

            if (onlyValid)
            {
                output = output.Where(a => a.Validate(compilation).Count == 0).ToList();
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

            // Create registrations for all attribute registrations
            // ([YarnCommand], [YarnFunction]). These registration are always
            // invoked. Separately, create registrations for all runtime
            // registrations (AddCommandHandler() invocations). These are only
            // invoked when the registration methods 'registrationType'
            // parameter equals Yarn.Unity.RegistrationType, which is true when
            // Yarn Spinner needs to list all commands and functions from
            // everywhere.
            //
            // We should only register runtime-registered commands when
            // explicitly requested, because otherwise if we register them here
            // AND during gameplay, they'll overlap and cause problems

            var attributeRegistrationStatements = actionGroups.SelectMany(group =>
            {

                var attributeRegistrations = group
                    .Where(a => a.DeclarationType != DeclarationType.DirectRegistration);
                return GetRegistrationStatements(attributeRegistrations);
            });

            var runtimeRegistrationStatements = actionGroups.SelectMany(group =>
            {
                var runtimeRegistrations = group
                    .Where(a => a.DeclarationType == DeclarationType.DirectRegistration);
                return GetRegistrationStatements(runtimeRegistrations);
            });

            var functionDeclarations = actionGroups.SelectMany(group =>
            {
                var functions = group
                    .Where(a => a.Type == ActionType.Function);
                return GetFunctionDeclarationStatements(functions);
            });

            // Create the method body that combines the attribute-registered and
            // (possibly) runtime-registered action registrations.
            var registrationMethodBody = SyntaxFactory.Block().WithStatements(SyntaxFactory.List<StatementSyntax>(
                attributeRegistrationStatements.Concat(functionDeclarations)
                ));

            // Create the list of attributes to attach to this method.
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
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
                    new[] {
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(targetParameterName)).WithType(SyntaxFactory.ParseTypeName("global::Yarn.Unity.IActionRegistration")),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(registrationTypeParameterName)).WithType(SyntaxFactory.ParseTypeName("Yarn.Unity.RegistrationType")),
                    }
                )), // parameters
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), // type parameter constraints
                registrationMethodBody, // body
                null, // arrow expression clause
                SyntaxFactory.Token(SyntaxKind.None) // semicolon token
                );

            return methodSyntax.NormalizeWhitespace();

            IEnumerable<StatementSyntax> GetRegistrationStatements(IEnumerable<Action> registerableCommands)
            {
                return registerableCommands
                    .Where(a => a.MethodSymbol?.MethodKind != MethodKind.AnonymousFunction)
                    .Select((a, i) =>
                    {
                        var registrationStatement = a.GetRegistrationSyntax(targetParameterName);
                        if (i == 0)
                        {
                            // Add a comment above the first registration that indicates where these actions came from
                            return registrationStatement.WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"// Actions from file:"),
                                    SyntaxFactory.Comment($"// {a.SourceFileName}")
                            ));
                        }
                        else
                        {
                            return registrationStatement;
                        }
                    }
                );
            }

            IEnumerable<StatementSyntax> GetFunctionDeclarationStatements(IEnumerable<Action> functions)
            {
                return functions
                    .Select((a, i) =>
                    {
                        var declarationStatement = a.GetFunctionDeclarationSyntax(targetParameterName);
                        if (i == 0)
                        {
                            // Add a comment above the first registration that indicates where these actions came from
                            return declarationStatement.WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"// Function declarations from file:"),
                                    SyntaxFactory.Comment($"// {a.SourceFileName}")
                            ));
                        }
                        else
                        {
                            return declarationStatement;
                        }
                    }
                );

            }
        }

        private static AttributeListSyntax GeneratedCodeAttributeList
        {
            get
            {
                // [System.CodeDom.Compiler.GeneratedCode(<toolName>, <toolVersion>)]

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

        public static IEnumerable<Action> GetActions(CSharpCompilation compilation, SyntaxTree tree, ILogger? yLogger = null)
        {
            var logger = yLogger;
            if (logger == null)
            {
                logger = new NullLogger();
            }

            var root = tree.GetCompilationUnitRoot();

            SemanticModel? model = null;

            if (compilation != null)
            {
                model = compilation.GetSemanticModel(tree, true);
            }

            if (model == null)
            {
                return Array.Empty<Action>();
            }

            return GetAttributeActions(root, model, logger).Concat(GetRuntimeDefinedActions(root, model));
        }

        private static IEnumerable<Action> GetRuntimeDefinedActions(CompilationUnitSyntax root, SemanticModel model)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            classes = classes.Where(c =>
            {
                var classAttributes = GetAttributes(c, model);

                bool hasGeneratedCodeAttribute = classAttributes.Any(attr =>
                {
                    var syntax = attr.Item1;
                    var data = attr.Item2;

                    // Check to see if this attribute is the [GeneratedCode]
                    // attribute
                    return (data?.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) == "global::System.CodeDom.Compiler.GeneratedCodeAttribute";
                });

                // Do not visit this class if it is generated code
                if (hasGeneratedCodeAttribute)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            });

            var methodInvocations = classes
                .SelectMany(classDecl => classDecl.DescendantNodes())
                .OfType<InvocationExpressionSyntax>()
                .Select(i =>
                {
                    // Get the symbol represening the method that's being called
                    var symbolInfo = model.GetSymbolInfo(i.Expression);

                    ISymbol? symbol = symbolInfo.Symbol;

                    if (symbol == null && symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                    {
                        // We weren't able to determine what specific method
                        // this was. Pick the first one - we don't actually need
                        // to know about what specific overload was used, since
                        // they all have a similar signature.
                        symbol = symbolInfo.CandidateSymbols.First();
                    }
                    var methodSymbol = symbol as IMethodSymbol;
                    return (Syntax: i, Symbol: methodSymbol);
                })
                .Where(i => i.Symbol != null)
                .DistinctBy(i => i.Syntax)
                .ToList();

            var dialogueRunnerCalls = methodInvocations
                .Where(info => info.Symbol?.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Yarn.Unity.DialogueRunner").ToList();

            var addCommandCalls = methodInvocations.Where(
                info => info.Symbol?.Name == "AddCommandHandler"
            ).Select(c => (Call: c, Type: ActionType.Command));

            var addFunctionCalls = methodInvocations.Where(
                info => info.Symbol?.Name == "AddFunction"
            ).Select(c => (Call: c, Type: ActionType.Function));

            var methodCalls = addCommandCalls.Concat(addFunctionCalls);

            foreach (var methodCall in methodCalls)
            {
                var (Syntax, Symbol) = methodCall.Call;

                var methodNameSyntax = Syntax.ArgumentList.Arguments.ElementAtOrDefault(0);
                var targetSyntax = Syntax.ArgumentList.Arguments.ElementAtOrDefault(1);
                if (methodNameSyntax == null || targetSyntax == null)
                {
                    continue;
                }

                if (!(model.GetConstantValue(methodNameSyntax.Expression).Value is string name))
                {
                    // TODO: handle case of 'we couldn't figure out the constant value here'
                    continue;
                }

                SymbolInfo targetSymbolInfo = model.GetSymbolInfo(targetSyntax.Expression);
                IMethodSymbol? targetSymbol = targetSymbolInfo.Symbol as IMethodSymbol;
                if (targetSymbol == null && targetSymbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                {
                    // We couldn't figure out exactly which of the targets to
                    // use. Choose one.
                    targetSymbol = targetSymbolInfo.CandidateSymbols.FirstOrDefault() as IMethodSymbol;
                }
                if (targetSymbol == null)
                {
                    // TODO: handle case of 'we couldn't figure out target method's
                    // symbol'
                    continue;
                }

                var declaringSyntax = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

                TryGetDocumentation(targetSymbol, out XElement? documentationXML, out string? summary);

                yield return new Action(name, methodCall.Type, targetSymbol)
                {
                    SemanticModel = model,
                    MethodName = targetSymbol.Name,
                    MethodDeclarationSyntax = declaringSyntax,
                    Declaration = declaringSyntax,
                    Description = summary,
                    Parameters = new List<Parameter>(GetParameters(targetSymbol, documentationXML)),
                    SourceFileName = root.SyntaxTree.FilePath,
                    DeclarationType = DeclarationType.DirectRegistration,
                };
            }
        }

        private static bool TryGetDocumentation(IMethodSymbol targetSymbol, out XElement? documentationXML, out string? summary)
        {
            documentationXML = null;
            summary = null;
            try
            {
                var documentationComments = targetSymbol.GetDocumentationCommentXml();
                documentationXML = XElement.Parse(documentationComments);
                var summaryNode = documentationXML.Element("summary");
                if (summaryNode != null)
                {
                    summary = string.Join("", summaryNode.DescendantNodes().OfType<XText>().Select(n => n.ToString())).Trim();
                }
                return true;
            }
            catch (System.Xml.XmlException)
            {
                // XML parse error; no documentation available
                return false;
            }
        }

        private static IEnumerable<Action> GetAttributeActions(CompilationUnitSyntax root, SemanticModel model, ILogger logger)
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
                .Where(pair => pair.Symbol != null);

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

                if (attr == null)
                {
                    // Not a valid action; skip
                    continue;
                }

                // working on an assumption that most people just use the method name
                string actionName = methodInfo.MethodDeclaration.Identifier.ToString();

                // handling the situation where they have provided arguments
                // if we have an argument list
                if (attr.ArgumentList != null)
                {
                    // we resolve the value of first item in that list
                    // and if it's a string we use that as the action name
                    var constantValue = model.GetConstantValue(attr.ArgumentList.Arguments.First().Expression);
                    if (constantValue.HasValue)
                    {
                        if (constantValue.Value is string constantString)
                        {
                            logger.WriteLine($"resolved constant expression value for the action name: {constantValue.Value.ToString()}");
                            actionName = constantString;
                        }
                        else
                        {
                            // Otherwise just logging the incorrect type and moving on with our life
                            logger.WriteLine($"resolved constant expression value for the action name, but it is not a string, skipping: {constantValue.Value}");
                        }
                    }
                }

                var position = methodInfo.MethodDeclaration.GetLocation();
                var lineIndex = position.GetLineSpan().StartLinePosition.Line + 1;

                var methodSymbol = methodInfo.Symbol;
                if (methodSymbol == null)
                {
                    logger.WriteLine($"Failed to get a symbol for " + methodInfo.MethodDeclaration.Identifier);
                    continue;
                }

                if (!(methodSymbol.ContainingSymbol is ITypeSymbol container))
                {
                    logger.WriteLine($"Failed to get a containing symbol for " + methodInfo.MethodDeclaration.Identifier);
                    continue;
                }

                TryGetDocumentation(methodSymbol, out XElement? documentationXML, out string? summary);

                var containerName = container?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "<unknown>";

                yield return new Action(actionName, methodInfo.ActionType, methodSymbol)
                {
                    Name = actionName,
                    Type = methodInfo.ActionType,
                    MethodName = $"{containerName}.{methodInfo.MethodDeclaration.Identifier}",
                    MethodIdentifierName = methodInfo.MethodDeclaration.Identifier.ToString(),
                    MethodSymbol = methodSymbol,
                    MethodDeclarationSyntax = methodInfo.MethodDeclaration,
                    IsStatic = methodSymbol.IsStatic,
                    Declaration = methodInfo.MethodDeclaration,
                    Parameters = new List<Parameter>(GetParameters(methodSymbol, documentationXML)),
                    AsyncType = GetAsyncType(methodSymbol),
                    SemanticModel = model,
                    Description = summary,
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

        private static IEnumerable<Parameter> GetParameters(IMethodSymbol symbol, XElement? documentationXML)
        {
            var parameterDocumentation = new Dictionary<string, string>();

            if (documentationXML != null)
            {
                var parameterNodes = documentationXML.Elements("param");
                foreach (var parameterNode in parameterNodes)
                {
                    var name = parameterNode.Attribute("name");
                    if (name == null) { continue; }
                    var text = string.Join(
                        "",
                        parameterNode.DescendantNodes().OfType<XText>().Select(v => v.Value)
                    ).Trim();

                    if (!parameterDocumentation.ContainsKey(name.Value))
                    {
                        parameterDocumentation.Add(name.Value, text);

                    }
                }
            }

            foreach (var param in symbol.Parameters)
            {
                parameterDocumentation.TryGetValue(param.Name, out var paramDoc);
                yield return new Parameter
                {
                    Name = param.Name,
                    IsOptional = param.IsOptional,
                    Type = param.Type,
                    Description = paramDoc,
                    IsParamsArray = param.IsParams,
                    DefaultValueString = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null,
                };
            }
        }

        internal static bool IsAttributeYarnCommand(AttributeData attribute)
        {
            return GetActionType(attribute) != ActionType.NotAnAction;
        }

        internal static ActionType GetActionType(SemanticModel model, MethodDeclarationSyntax decl, out AttributeSyntax? actionAttribute)
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
                return ActionType.Invalid;
            }

            var actionType = actionTypes.Single();
            actionAttribute = actionType.Syntax;
            return actionType.Type;
        }

        internal static ActionType GetActionType(AttributeData attr)
        {
            INamedTypeSymbol? attributeClass = attr.AttributeClass;

            switch (attributeClass?.Name)
            {
                case "YarnCommandAttribute": return ActionType.Command;
                case "YarnFunctionAttribute": return ActionType.Function;
                default: return ActionType.NotAnAction;
            }
        }

        public static IEnumerable<(AttributeSyntax, AttributeData)> GetAttributes(ClassDeclarationSyntax classDecl, SemanticModel model)
        {
            INamedTypeSymbol? classSymbol = model.GetDeclaredSymbol(classDecl);

            var methodAttributes = classSymbol?.GetAttributes();

            if (methodAttributes == null)
            {
                yield break;
            }

            foreach (var attribute in methodAttributes)
            {
                if (attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax syntax)
                {
                    yield return (syntax, attribute);
                }
            }
        }

        public static IEnumerable<(AttributeSyntax, AttributeData)> GetAttributes(MethodDeclarationSyntax method, SemanticModel model)
        {
            IMethodSymbol? methodSymbol = model.GetDeclaredSymbol(method);

            var methodAttributes = methodSymbol?.GetAttributes();

            if (methodAttributes == null)
            {
                yield break;
            }

            foreach (var attribute in methodAttributes)
            {
                if (attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax syntax)
                {
                    yield return (syntax, attribute);
                }
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
            throw new FileNotFoundException($"No file or directory at {sourcePath} was found.", sourcePath);
        }

        public static Type? GetTypeByName(string name)
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

        // these are basically just ripped straight from the LSP
        // should maybe look at making these more accessible, for now the code dupe is fine IMO
        public static string? GetActionTrivia(MethodDeclarationSyntax method, ILogger logger)
        {
            // The main string to use as the function's documentation.
            if (method.HasLeadingTrivia)
            {
                var trivias = method.GetLeadingTrivia();
                var structuredTrivia = trivias.LastOrDefault(t => t.HasStructure);
                if (structuredTrivia.Kind() != SyntaxKind.None)
                {
                    // The method contains structured trivia. Extract the
                    // documentation for it.
                    logger.WriteLine("trivia is structured");
                    return GetDocumentationFromStructuredTrivia(structuredTrivia);
                }
                else
                {
                    // There isn't any structured trivia, but perhaps there's a
                    // comment above the method, which we can use as our
                    // documentation.
                    logger.WriteLine("trivia is unstructured");
                    return GetDocumentationFromUnstructuredTrivia(trivias);
                }
            }
            else
            {
                return null;
            }
        }
        private static string GetDocumentationFromUnstructuredTrivia(SyntaxTriviaList trivias)
        {
            string documentation;
            bool emptyLineFlag = false;
            var documentationParts = Enumerable.Empty<string>();

            // loop in reverse order until hit something that doesn't look like it's related
            foreach (var trivia in trivias.Reverse())
            {
                var doneWithTrivia = false;
                switch (trivia.Kind())
                {
                    case SyntaxKind.EndOfLineTrivia:
                        // if we hit two lines in a row without a comment/attribute inbetween, we're done collecting trivia
                        if (emptyLineFlag == true) { doneWithTrivia = true; }
                        emptyLineFlag = true;
                        break;
                    case SyntaxKind.WhitespaceTrivia:
                        break;
                    case SyntaxKind.Attribute:
                        emptyLineFlag = false;
                        break;
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                        documentationParts = documentationParts.Prepend(trivia.ToString().Trim('/', ' '));
                        emptyLineFlag = false;
                        break;
                    default:
                        doneWithTrivia = true;
                        break;
                }

                if (doneWithTrivia)
                {
                    break;
                }
            }

            documentation = string.Join(" ", documentationParts);
            return documentation;
        }
        private static string? GetDocumentationFromStructuredTrivia(SyntaxTrivia structuredTrivia)
        {
            string documentation;
            var triviaStructure = structuredTrivia.GetStructure();
            if (triviaStructure == null)
            {
                return null;
            }

            string? ExtractStructuredTrivia(string tagName)
            {
                // Find the tag that matches the requested name.
                var triviaMatch = triviaStructure
                    .ChildNodes()
                    .OfType<XmlElementSyntax>()
                    .FirstOrDefault(x =>
                        x.StartTag.Name.ToString() == tagName
                    );

                if (triviaMatch != null
                    && triviaMatch.Kind() != SyntaxKind.None
                    && triviaMatch.Content.Any())
                {
                    // Get all content from this element that isn't a newline, and
                    // join it up into a single string.
                    var v = triviaMatch
                        .Content[0]
                        .ChildTokens()
                        .Where(ct => ct.Kind() != SyntaxKind.XmlTextLiteralNewLineToken)
                        .Select(ct => ct.ValueText.Trim());

                    return string.Join(" ", v).Trim();
                }

                return null;
            }

            var summary = ExtractStructuredTrivia("summary");
            var remarks = ExtractStructuredTrivia("remarks");

            documentation = summary ?? triviaStructure.ToString();

            if (remarks != null)
            {
                documentation += "\n\n" + remarks;
            }

            return documentation;
        }
    }
}
