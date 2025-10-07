/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Yarn.Unity.ActionAnalyser
{

    public struct Position
    {
        public int Line;
        public int Column;
    }


    public struct Range
    {
        public Position Start;
        public Position End;

        public static implicit operator Range(FileLinePositionSpan span)
        {
            var start = span.StartLinePosition;
            var end = span.EndLinePosition;

            return new Range
            {
                Start = {
                Line = start.Line + 1,
                Column = start.Character + 1,
            },
                End = {
                Line = end.Line + 1,
                Column = end.Character + 1,
            },
            };
        }
    }


    public enum ActionType
    {
        /// <summary>
        /// The method represents a command.
        /// </summary>
        Command,
        /// <summary>
        /// The method represents a function.
        /// </summary>
        Function,
        /// <summary>
        /// The method may have been intended to be an action, but its type
        /// cannot be determined.
        /// </summary>
        Invalid,
        /// <summary>
        /// The method is not a Yarn action.
        /// </summary>

        NotAnAction,
    }

    public enum DeclarationType
    {
        /// <summary>
        /// The action is declared via a YarnCommand or YarnFunction attribute.
        /// </summary>
        Attribute,
        /// <summary>
        /// The action is declared by calling AddCommandHandler or AddFunction
        /// on a DialogueRunner.
        /// </summary>
        DirectRegistration
    }

    public enum AsyncType
    {
        /// <summary>
        /// The action operates synchronously.
        /// </summary>
        Sync,
        /// <summary>
        /// The action may operate asynchronously, and Dialogue Runners should
        /// check the return value of the action to determine whether to block
        /// on the method call or not.
        /// </summary>
        /// <remarks>
        /// This is only valid for <see cref="Action"/> objects whose <see
        /// cref="Action.Type"/> is <see cref="ActionType.Command"/>.
        /// </remarks>
        MaybeAsyncCoroutine,
        /// <summary>
        /// The action operates asynchronously using a coroutine.
        /// </summary>
        AsyncCoroutine,
    }

    static class ITypeSymbolExtension
    {
        public static string? GetYarnTypeString(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.SpecialType switch
            {
                SpecialType.System_Boolean => "bool",
                SpecialType.System_SByte => "number",
                SpecialType.System_Byte => "number",
                SpecialType.System_Int16 => "number",
                SpecialType.System_UInt16 => "number",
                SpecialType.System_Int32 => "number",
                SpecialType.System_UInt32 => "number",
                SpecialType.System_Int64 => "number",
                SpecialType.System_UInt64 => "number",
                SpecialType.System_Decimal => "number",
                SpecialType.System_Single => "number",
                SpecialType.System_Double => "number",
                SpecialType.System_String => "string",
                _ => null
            };
        }
    }

    public struct Parameter
    {
        public bool IsOptional;
        public string Name;
        public ITypeSymbol Type;
        public string? Description;
        public string? DefaultValueString;
        public bool IsParamsArray;

        public readonly string? YarnTypeString => Type.GetYarnTypeString();
    }

    public class Action
    {
        public Action(string name, ActionType type, IMethodSymbol methodSymbol)
        {
            Name = name;
            Type = type;
            MethodSymbol = methodSymbol;
        }

        /// <summary>
        /// The name of this action.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The method symbol for this action.
        /// </summary>
        public IMethodSymbol MethodSymbol { get; internal set; }

        public string? Description { get; internal set; }

        /// <summary>
        /// The declaration of this action's method, if available.
        /// </summary>
        public SyntaxNode? Declaration { get; internal set; }

        /// <summary>
        /// The type of the action.
        /// </summary>
        public ActionType Type { get; internal set; }

        /// <summary>
        /// The declaration type of the action.
        /// </summary>
        public DeclarationType DeclarationType { get; internal set; }

        /// <summary>
        /// The sync/async type of the action.
        /// </summary>
        public AsyncType AsyncType { get; internal set; }

        /// <summary>
        /// The <see cref="Microsoft.CodeAnalysis.SemanticModel"/> that can be
        /// used to answer semantic queries about this method.
        /// </summary>
        internal SemanticModel? SemanticModel { get; set; }

        /// <summary>
        /// The fully-qualified name for this method, including the global
        /// prefix.
        /// </summary>
        public string? MethodName { get; set; }

        /// <summary>
        /// Gets the short form of the method, essentially the easy to read form of <see cref="MethodName"/>.
        /// </summary>
        public string? MethodIdentifierName { get; internal set; }

        /// <summary>
        /// Whether this action is a static method, or an instance method.
        /// </summary>
        public bool IsStatic { get; internal set; }

        /// <summary>
        /// Gets the path to the file that this action was declared in.
        /// </summary>
        public string? SourceFileName { get; internal set; }

        /// <summary>
        /// The syntax node for the method declaration associated with this action.
        /// </summary>
        public SyntaxNode? MethodDeclarationSyntax { get; internal set; }

        // The names of the methods that register commands and functions
        private const string AddCommandHandlerMethodName = "AddCommandHandler";
        private const string AddFunctionMethodName = "AddFunction";
        private const string RegisterFunctionDeclarationName = "RegisterFunctionDeclaration";

        /// <summary>
        /// The list of parameters that this action takes.
        /// </summary>
        public List<Parameter> Parameters = new List<Parameter>();

        public string? YarnReturnTypeString => this.MethodSymbol.ReturnType.GetYarnTypeString();

        public string ToJSON()
        {
            var result = new Dictionary<string, object?>();

            result["YarnName"] = this.Name;
            result["DefinitionName"] = this.MethodName;
            result["FileName"] = this.SourceFileName;
            if (!string.IsNullOrEmpty(this.Description))
            {
                result["Documentation"] = this.Description;
            }
            result["Language"] = "csharp";

            if (this.Declaration != null)
            {
                var location = this.Declaration.GetLocation().GetLineSpan();

                var startPosition = new Dictionary<string, int>()
                {
                    {"line", location.StartLinePosition.Line},
                    {"character", location.StartLinePosition.Character},
                };
                var endPosition = new Dictionary<string, int>()
                {
                    {"line", location.EndLinePosition.Line},
                    {"character", location.EndLinePosition.Character},
                };
                result["Location"] = new Dictionary<string, Dictionary<string, int>>()
                {
                    {"start", startPosition},
                    {"end", endPosition},
                };
            }

            result["Parameters"] = new List<Dictionary<string, object?>>(this.Parameters.Select(p =>
            {
                var paramObject = new Dictionary<string, object?>();

                paramObject["Name"] = p.Name;
                if (!string.IsNullOrEmpty(p.Description))
                {
                    paramObject["Documentation"] = p.Description;
                }
                if (!string.IsNullOrEmpty(p.DefaultValueString))
                {
                    paramObject["DefaultValue"] = p.DefaultValueString;
                }
                paramObject["IsParamsArray"] = p.IsParamsArray;
                paramObject["Type"] = p.YarnTypeString;

                return paramObject;
            }).ToArray());

            if (this.Type == ActionType.Function)
            {
                result["ReturnType"] = this.YarnReturnTypeString;
            }

            return Yarn.Unity.Editor.Json.Serialize(result);
        }

        public List<Microsoft.CodeAnalysis.Diagnostic> Validate(Compilation compilation)
        {
            var diagnostics = new List<Microsoft.CodeAnalysis.Diagnostic>();
            if (this.MethodDeclarationSyntax == null)
            {
                // No declaration syntax - we have nowhere to attach any diagnostics to
                return diagnostics;
            }

            Location diagnosticLocation;
            string identifier;

            if (this.Declaration is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                diagnosticLocation = methodDeclarationSyntax.Identifier.GetLocation();
                identifier = methodDeclarationSyntax.Identifier.ToString();
            }
            else
            {
                diagnosticLocation = this.MethodDeclarationSyntax.GetLocation();
                identifier = "(anonymous function)";
            }

            // Commands are parsed as whitespace, so spaces in the command name
            // would render the command un-callable.
            if (Name.Any(x => Char.IsWhiteSpace(x)))
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.YS1002ActionMethodsMustHaveAValidName, this.MethodDeclarationSyntax.GetLocation(), this.Name));
            }

            if (this.Name == null)
            {
                throw new NullReferenceException("Action name is null");
            }

            if (this.MethodSymbol == null)
            {
                throw new NullReferenceException($"Method symbol for {Name} is null");
            }

            // Actions that are registered via an attribute must be publicly
            // accessible
            if (this.DeclarationType == DeclarationType.Attribute)
            {
                if (MethodSymbol.DeclaredAccessibility != Accessibility.Public)
                {
                    // The method is not public
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.YS1001ActionMethodsMustBePublic,
                        diagnosticLocation, identifier, MethodSymbol.DeclaredAccessibility));
                }
                else
                {
                    var containingType = MethodSymbol.ContainingType;

                    while (containingType != null)
                    {
                        if (containingType.DeclaredAccessibility != Accessibility.Public)
                        {
                            // The method is public, but it's within a type that
                            // is not
                            var typeName = containingType.Name ?? "(anonymous)";
                            diagnostics.Add(Diagnostic.Create(
                                Diagnostics.YS1007ActionsMustBeInPublicTypes,
                                diagnosticLocation, identifier, typeName, containingType.DeclaredAccessibility));
                            break;
                        }
                        containingType = containingType.ContainingType;
                    }

                }
            }

            switch (Type)
            {
                case ActionType.Invalid:
                    {
                        var actionAttributes = MethodSymbol.GetAttributes().Where(attr => Analyser.IsAttributeYarnCommand(attr));

                        var count = actionAttributes.Count();

                        if (count != 1)
                        {
                            diagnostics.Add(Diagnostic.Create(Diagnostics.YS1005ActionMethodsMustHaveOneActionAttribute, diagnosticLocation, 0));
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic.Create(Diagnostics.YS1000UnknownError, diagnosticLocation, "Method marked as 'not an action' but it had one attribute"));
                        }
                    }
                    break;

                case ActionType.Command:
                    diagnostics.AddRange(ValidateCommand(compilation));
                    break;

                case ActionType.Function:
                    diagnostics.AddRange(ValidateFunction(compilation));
                    break;

                default:
                    diagnostics.Add(Diagnostic.Create(Diagnostics.YS1000UnknownError, diagnosticLocation, $"Internal error: invalid type {Type}"));
                    break;
            }

            return diagnostics;
        }

        private IEnumerable<Diagnostic> ValidateFunction(Compilation compilation)
        {

            string identifier;
            Location returnTypeLocation;
            Location identifierLocation;

            if (this.Declaration == null)
            {
                // No declaration - we can't attach any diagnostics
                yield break;
            }

            if (this.Declaration is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                identifierLocation = methodDeclarationSyntax.Identifier.GetLocation();
                returnTypeLocation = methodDeclarationSyntax.ReturnType.GetLocation();
                identifier = methodDeclarationSyntax.Identifier.ToString();
            }
            else
            {
                identifierLocation = Declaration.GetLocation();
                returnTypeLocation = this.Declaration.GetLocation();
                identifier = "(anonymous function)";
            }

            if (this.MethodSymbol == null)
            {
                throw new NotImplementedException("Todo: handle case where action's method is not a IMethodSymbol");
            }

            // Functions must be static
            if (this.MethodSymbol.MethodKind == MethodKind.Ordinary && this.MethodSymbol.IsStatic == false)
            {
                yield return Diagnostic.Create(Diagnostics.YS1006YarnFunctionsMustBeStatic, identifierLocation);
            }

            // Functions must return a number, string, or bool
            var returnTypeSymbol = this.MethodSymbol.ReturnType;

            switch (returnTypeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    break;
                default:
                    yield return Diagnostic.Create(Diagnostics.YS1004FunctionMethodsMustHaveAValidReturnType, returnTypeLocation, identifier, returnTypeSymbol.ToString());
                    break;

            }
        }

        private IEnumerable<Diagnostic> ValidateCommand(Compilation compilation)
        {
            if (MethodSymbol == null)
            {
                throw new NullReferenceException("Method symbol is null");
            }

            List<ITypeSymbol> validCommandReturnTypes = new List<ITypeSymbol?> {
                    compilation.GetTypeByMetadataName("UnityEngine.Coroutine"),
                    compilation.GetTypeByMetadataName("System.Collections.IEnumerator"),
                    compilation.GetSpecialType(SpecialType.System_Void),
                }
                .NonNull(throwIfAnyNull: true)
                .ToList();

            List<ITypeSymbol> validTaskTypes = new List<ITypeSymbol?> {
                    compilation.GetTypeByMetadataName("System.Threading.Tasks.Task"),
                    compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask"),
                    compilation.GetTypeByMetadataName("UnityEngine.Awaitable"),
                    compilation.GetTypeByMetadataName("Yarn.Unity.YarnTask"),
            }.NonNull(throwIfAnyNull: false)
            .ToList();

            // Explicitly ban 'string' as a return type - strings implement
            // IEnumerator, but they're not coroutines. We'll need to manually
            // exclude this.
            List<ITypeSymbol> knownInvalidCommandReturnTypes = new List<ITypeSymbol?> {
                    compilation.GetSpecialType(SpecialType.System_String),
                }
                .NonNull(throwIfAnyNull: true)
                .ToList();

            // Functions must return void, IEnumerator, Coroutine, or an awaitable type
            var returnTypeSymbol = MethodSymbol.ReturnType;

            Location returnTypeLocation;
            string identifier;
            string returnTypeName;
            if (this.MethodDeclarationSyntax is MethodDeclarationSyntax methodDeclaration)
            {
                returnTypeLocation = methodDeclaration.ReturnType.GetLocation();
                identifier = methodDeclaration.Identifier.ToString();
                returnTypeName = methodDeclaration.ReturnType.ToString();
            }
            else if (this.MethodDeclarationSyntax is LocalFunctionStatementSyntax localFunctionStatement)
            {
                returnTypeLocation = localFunctionStatement.ReturnType.GetLocation();
                identifier = localFunctionStatement.Identifier.ToString();
                returnTypeName = localFunctionStatement.ReturnType.ToString();
            }
            else if (this.MethodDeclarationSyntax is LambdaExpressionSyntax lambdaExpression)
            {
                returnTypeLocation = lambdaExpression.GetLocation();
                identifier = "(lambda expression)";
                returnTypeName = returnTypeSymbol.Name;
            }
            else
            {
                throw new InvalidOperationException($"Expected decl for {this.Name} ({this.SourceFileName}) was of unexpected type {this.MethodDeclarationSyntax?.GetType().Name ?? "null"}");
            }


            var typeIsKnownValid = validCommandReturnTypes.Contains(returnTypeSymbol)
                || validTaskTypes.Contains(returnTypeSymbol);
            var typeIsKnownInvalid = knownInvalidCommandReturnTypes.Contains(returnTypeSymbol);

            var returnTypeIsValid = typeIsKnownValid && !typeIsKnownInvalid;

            if (returnTypeIsValid == false)
            {
                yield return Diagnostic.Create(Diagnostics.YS1003CommandMethodsMustHaveAValidReturnType,
                                               returnTypeLocation,
                                               identifier,
                                               returnTypeName);
            }
        }

        public StatementSyntax GetRegistrationSyntax(string dialogueRunnerVariableName = "dialogueRunner")
        {
            if (MethodSymbol == null)
            {
                throw new NullReferenceException("Method symbol is null");
            }
            if (Name == null)
            {
                throw new NullReferenceException("Action name is null");
            }
            string registrationMethodName;
            switch (Type)
            {
                case ActionType.Command:
                    registrationMethodName = AddCommandHandlerMethodName;
                    break;
                case ActionType.Function:
                    registrationMethodName = AddFunctionMethodName;
                    break;
                default:
                    throw new InvalidOperationException($"Action {Name} is not a valid action");
            }

            SimpleNameSyntax nameSyntax;

            // Get any parameters we have for this method as a sequence of type
            // symbols. We'll use that when building the call to
            // AddCommandHandler/Function.
            var parameterTypes = (MethodSymbol as IMethodSymbol)?.Parameters.Select(p => p.Type) ?? Enumerable.Empty<ITypeSymbol>();

            var typeArguments = parameterTypes.Select(t =>
            {
                return SyntaxFactory.ParseTypeName(t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            });

            // If this is a function, we also need to include the return type in
            // this list.
            if (Type == ActionType.Function)
            {
                var returnType = (MethodSymbol as IMethodSymbol)?.ReturnType ?? throw new InvalidOperationException($"Action {Name} has type {ActionType.Function}, but its return type is null.");

                typeArguments = typeArguments.Append(SyntaxFactory.ParseTypeName(returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }

            if (typeArguments.Any() && MethodSymbol?.IsStatic == true)
            {
                // This method needs to be specified with type arguments, so
                // we'll need to call the appropriate generic version of
                // AddCommandHandler/Function that takes type parameters. Create
                // a new GenericName for AddCommandHandler/Function and provide
                // it with the type parameter list that we just built.

                nameSyntax = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(registrationMethodName),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments))
                );
            }
            else
            {
                // This method doesn't need to specify any type parameters, so
                // we can just use the identifier name.
                nameSyntax = SyntaxFactory.IdentifierName(registrationMethodName);
            }

            // Create the expression that refers to the
            // 'AddCommandHandler/Function' instance method on the dialogue
            // runner variable name we were provided.
            var addCommandHandlerExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(dialogueRunnerVariableName),
                SyntaxFactory.Token(SyntaxKind.DotToken),
                nameSyntax
                );

            ExpressionSyntax methodReferenceExpression = GetReferenceSyntaxForRegistration();

            var arguments = SyntaxFactory.ArgumentList().AddArguments(new[]{
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(this.Name)
                    )
                ),
                SyntaxFactory.Argument(methodReferenceExpression)
                    .WithLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ")),
            });

            var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(addCommandHandlerExpression, arguments);

            var invocationStatement = SyntaxFactory.ExpressionStatement(invocationExpressionSyntax);

            return invocationStatement;
        }

        public ExpressionSyntax GetReferenceSyntaxForRegistration()
        {
            // Create an expression that refers to the type that contains the
            // method we're registering.
            var containingTypeExpression = SyntaxFactory.ParseName(MethodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            // Now use that to create an expression that refers to _this method group_
            // on _that type_.
            var methodReference = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                containingTypeExpression,
                SyntaxFactory.IdentifierName(MethodSymbol.Name)
            );

            if (IsStatic)
            {

                // If the method is static, we can use the reference to the method directly.
                return methodReference;

            }
            else
            {
                // If the method is not static, we must create a MethodInfo for this method, like this:
                // typeof(ContainingType)
                //    .GetMethod(nameof(ContainingType.Method), 
                //               new[] { typeof(MethodParam1), typeof(MethodParam2)} )

                // Create an expression that gets a MethodInfo for the action's method.

                var typeOfContainingTypeExpression = SyntaxFactory.TypeOfExpression(containingTypeExpression);

                const string nameOfIdentifier = "nameof";
                const string getMethodIdentifier = "GetMethod";

                var typeOfMethodParameters = MethodSymbol.Parameters.Select(p =>
                {
                    string typeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    TypeSyntax type = SyntaxFactory.ParseTypeName(typeName);
                    return SyntaxFactory.TypeOfExpression(type);
                });

                ExpressionSyntax nameOfMethod;

                if (MethodSymbol.DeclaredAccessibility != Accessibility.Public)
                {
                    // The method is not public, so we can't use nameof() on it,
                    // because it would cause a compiler error. Instead, we'll have to
                    // refer to the method by name.
                    nameOfMethod = SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(MethodName ?? MethodSymbol.Name)
                    );
                }
                else
                {
                    // The method is public, so we can use nameof() to refer to
                    // it in a more durable way.

                    nameOfMethod = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseName(nameOfIdentifier),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                new[] {
                                    SyntaxFactory.Argument(methodReference)
                                }
                            )
                        )
                    );
                }


                var arrayOfTypeParameters = SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(
                        SyntaxFactory.ParseTypeName("System.Type"),
                        SyntaxFactory.List(
                            new[] {
                                SyntaxFactory.ArrayRankSpecifier(
                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                        SyntaxFactory.OmittedArraySizeExpression()
                                    )
                                )
                            }
                        )
                    ),
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,

                        SyntaxFactory.SeparatedList<ExpressionSyntax>(
                            typeOfMethodParameters
                        )
                    )
                );

                var getMethod = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    typeOfContainingTypeExpression,
                    SyntaxFactory.IdentifierName(getMethodIdentifier)
                );

                var getMethodArguments = SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        new[] {
                            SyntaxFactory.Argument(nameOfMethod),
                            SyntaxFactory.Argument(arrayOfTypeParameters)
                        }
                    )
                );

                var getMethodInvocation = SyntaxFactory.InvocationExpression(getMethod, getMethodArguments);

                return getMethodInvocation;
            }
        }

        public StatementSyntax GetFunctionDeclarationSyntax(string dialogueRunnerVariableName = "dialogueRunner")
        {
            var typeOfMethodReturn = SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(MethodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            var typeOfMethodParameters = MethodSymbol.Parameters.Select(p =>
            {
                string typeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                TypeSyntax type = SyntaxFactory.ParseTypeName(typeName);
                return SyntaxFactory.TypeOfExpression(type);
            });

            var arrayOfTypeParameters = SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(
                        SyntaxFactory.ParseTypeName("System.Type"),
                        SyntaxFactory.List(
                            new[] {
                                SyntaxFactory.ArrayRankSpecifier(
                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                        SyntaxFactory.OmittedArraySizeExpression()
                                    )
                                )
                            }
                        )
                    ),
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,

                        SyntaxFactory.SeparatedList<ExpressionSyntax>(
                            typeOfMethodParameters
                        )
                    )
                );

            var argumentsToRegisterCall = SyntaxFactory.ArgumentList().AddArguments(new[]{
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(this.Name)
                    )
                ),
                SyntaxFactory.Argument(typeOfMethodReturn),
                SyntaxFactory.Argument(arrayOfTypeParameters)
            });

            // Create the expression that refers to the
            // 'RegisterFunctionDeclaration' instance method on the dialogue
            // runner variable name we were provided.
            var registerFunctionMethodAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(dialogueRunnerVariableName),
                SyntaxFactory.Token(SyntaxKind.DotToken),
                SyntaxFactory.IdentifierName(RegisterFunctionDeclarationName)
                );

            var registerFunctionMethodInvocation = SyntaxFactory.InvocationExpression(registerFunctionMethodAccess, argumentsToRegisterCall);

            var invocationStatement = SyntaxFactory.ExpressionStatement(registerFunctionMethodInvocation);

            return invocationStatement;
        }
    }
}
