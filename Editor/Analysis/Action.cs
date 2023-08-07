using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    public struct Parameter
    {
        public bool IsOptional;
        public string Name;
        public ITypeSymbol Type;
    }

    public class Diagnostic
    {
        public Diagnostic(string message, SyntaxNode node)
        {
            this.Message = message;

            this.Range = node.GetLocation().GetLineSpan();
        }

        public Diagnostic(string message, SyntaxToken token)
        {
            this.Message = message;

            this.Range = token.GetLocation().GetLineSpan();
        }

        public Range Range { get; private set; }

        public string Message { get; private set; }
    }

    public class Action
    {
        /// <summary>
        /// The name of this action.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The method symbol for this action.
        /// </summary>
        public IMethodSymbol MethodSymbol { get; internal set; }

        /// <summary>
        /// The declaration of this action's method, if available.
        /// </summary>
        public SyntaxNode Declaration { get; internal set; }

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
        internal SemanticModel SemanticModel { get; set; }

        /// <summary>
        /// The fully-qualified name for this method, including the global
        /// prefix.
        /// </summary>
        public string MethodName { get; internal set; }

        /// <summary>
        /// Whether this action is a static method, or an instance method.
        /// </summary>
        public bool IsStatic { get; internal set; }

        /// <summary>
        /// Gets the path to the file that this action was declared in.
        /// </summary>
        public string SourceFileName { get; internal set; }

        // The names of the methods that register commands and functions
        private const string AddCommandHandlerMethodName = "AddCommandHandler";
        private const string AddFunctionMethodName = "AddFunction";

        /// <summary>
        /// The list of parameters that this action takes.
        /// </summary>
        public List<Parameter> Parameters = new List<Parameter>();

        public bool Validate(out Diagnostic failureReason)
        {
            var methodDeclaration = Declaration as MethodDeclarationSyntax;

            if (methodDeclaration == null)
            {
                throw new NotImplementedException("Todo: handle case where action's method is not a MethodDeclaration");
            }

            switch (Type)
            {
                case ActionType.NotAnAction:
                    {
                        var actionAttributes = MethodSymbol.GetAttributes().Where(attr => Analyser.IsAttributeYarnCommand(attr));

                        var count = actionAttributes.Count();

                        if (count == 0)
                        {
                            failureReason = new Diagnostic("Actions require a YarnCommand or YarnFunction attribute", methodDeclaration.Identifier);
                        }
                        else if (count > 1)
                        {
                            failureReason = new Diagnostic("Actions can only have one YarnCommand or YarnFunction attribute", methodDeclaration.Identifier);
                        }
                        else
                        {
                            failureReason = new Diagnostic("Internal error: unknown reason", methodDeclaration.Identifier);
                        }

                        return false;
                    }

                case ActionType.Command:
                    return ValidateCommand(out failureReason);

                case ActionType.Function:
                    return ValidateFunction(out failureReason);

                default:
                    failureReason = new Diagnostic($"Internal error: invalid type {Type}", methodDeclaration.Identifier);
                    return false;
            }
        }

        private bool ValidateFunction(out Diagnostic failureReason)
        {
            var methodDeclaration = Declaration as MethodDeclarationSyntax;

            if (methodDeclaration == null)
            {
                throw new NotImplementedException("Todo: handle case where action's method is not a MethodDeclaration");
            }

            var methodSymbol = this.MethodSymbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                throw new NotImplementedException("Todo: handle case where action's method is not a IMethodSymbol");
            }

            // Functions must be static
            if (methodSymbol.IsStatic == false)
            {
                failureReason = new Diagnostic("Yarn functions must be static", methodDeclaration.Identifier);
                return false;
            }

            // Functions must return a number, string, or bool
            var returnTypeSymbol = methodSymbol.ReturnType;

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
                    failureReason = new Diagnostic("Functions must return numbers, strings, or bools", methodDeclaration.ReturnType);
                    return false;

            }

            failureReason = null;
            return true;
        }

        private bool ValidateCommand(out Diagnostic failureReason)
        {
            throw new NotImplementedException();
        }

        public SyntaxNode GetRegistrationSyntax(string dialogueRunnerVariableName = "dialogueRunner")
        {
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

            if (typeArguments.Any() && MethodSymbol.IsStatic == true)
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

        public ExpressionSyntax GetReferenceSyntaxForRegistration() {
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

            if (IsStatic) {
                
                // If the method is static, we can use the reference to the method directly.
                return methodReference;

            } else {
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

                var nameOfMethod = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseName(nameOfIdentifier), 
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new[] { 
                                SyntaxFactory.Argument(methodReference) 
                            }
                        )
                    )
                );

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
    }
}
