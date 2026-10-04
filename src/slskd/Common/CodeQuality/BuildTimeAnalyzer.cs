// <copyright file="BuildTimeAnalyzer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.CodeQuality
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     Build-time code analysis using Roslyn syntax trees.
    /// </summary>
    /// <remarks>
    ///     H-CODE02: Introduce Static Analysis and Linting.
    ///     Provides compile-time analysis for security and quality issues.
    /// </remarks>
    public static class BuildTimeAnalyzer
    {
        /// <summary>
        ///     Analyzes C# source code for violations.
        /// </summary>
        /// <param name="sourceCode">The C# source code to analyze.</param>
        /// <param name="filePath">The file path for error reporting.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>List of violations found in the source code.</returns>
        public static IEnumerable<CodeAnalysisViolation> AnalyzeSourceCode(string sourceCode, string filePath, ILogger? logger = null)
        {
            var violations = new List<CodeAnalysisViolation>();

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                // Analyze for blocking async calls
                violations.AddRange(AnalyzeBlockingAsyncCalls(root, filePath));

                // Analyze for insecure string operations
                violations.AddRange(AnalyzeInsecureStringOperations(root, filePath));

                // Analyze for improper exception handling
                violations.AddRange(AnalyzeExceptionHandling(root, filePath));

                // Analyze for security issues
                violations.AddRange(AnalyzeSecurityIssues(root, filePath));

            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to analyze source file {FilePath}", filePath);
            }

            return violations;
        }

        private static IEnumerable<CodeAnalysisViolation> AnalyzeBlockingAsyncCalls(SyntaxNode root, string filePath)
        {
            var violations = new List<CodeAnalysisViolation>();

            // A property named Result is common in MVC and binding APIs. Only
            // report it when source syntax identifies the receiver as Task-like.
            var resultCalls = root.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Where(m => m.Name.Identifier.Text == "Result" && IsTaskLikeExpression(m.Expression, root));

            foreach (var call in resultCalls)
            {
                violations.Add(new CodeAnalysisViolation
                {
                    FilePath = filePath,
                    LineNumber = call.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Rule = "BlockingAsyncCall",
                    Severity = ViolationSeverity.Error,
                    Message = "Blocking async call detected (.Result)",
                    CodeSnippet = call.ToString(),
                    Recommendation = "Use 'await' instead of .Result to avoid deadlocks"
                });
            }

            // Find .Wait() calls
            var waitCalls = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression is MemberAccessExpressionSyntax m &&
                           m.Name.Identifier.Text == "Wait" &&
                           m.Name is not GenericNameSyntax &&
                           IsTaskLikeExpression(m.Expression, root) &&
                           !HasZeroTimeout(i));

            foreach (var call in waitCalls)
            {
                violations.Add(new CodeAnalysisViolation
                {
                    FilePath = filePath,
                    LineNumber = call.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Rule = "BlockingAsyncCall",
                    Severity = ViolationSeverity.Error,
                    Message = "Blocking async call detected (.Wait())",
                    CodeSnippet = call.ToString(),
                    Recommendation = "Use 'await' instead of .Wait() to avoid deadlocks"
                });
            }

            // Find .GetAwaiter().GetResult() calls
            var getAwaiterCalls = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression is MemberAccessExpressionSyntax m &&
                           m.Name.Identifier.Text == "GetResult" &&
                           m.Expression is InvocationExpressionSyntax inner &&
                           inner.Expression is MemberAccessExpressionSyntax innerMember &&
                           innerMember.Name.Identifier.Text == "GetAwaiter" &&
                           IsTaskLikeExpression(innerMember.Expression, root));

            foreach (var call in getAwaiterCalls)
            {
                violations.Add(new CodeAnalysisViolation
                {
                    FilePath = filePath,
                    LineNumber = call.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Rule = "BlockingAsyncCall",
                    Severity = ViolationSeverity.Error,
                    Message = "Blocking async call detected (.GetAwaiter().GetResult())",
                    CodeSnippet = call.ToString(),
                    Recommendation = "Use 'await' instead of .GetAwaiter().GetResult()"
                });
            }

            return violations;
        }

        private static bool IsTaskLikeExpression(ExpressionSyntax expression, SyntaxNode root)
        {
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return IsTaskLikeExpression(parenthesized.Expression, root);
            }

            if (expression is ObjectCreationExpressionSyntax objectCreation)
            {
                return IsTaskLikeType(objectCreation.Type);
            }

            if (expression is InvocationExpressionSyntax invocation)
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    var methodName = invocation.Expression is IdentifierNameSyntax identifier
                        ? identifier.Identifier.ValueText
                        : null;
                    return IsDeclaredTaskLikeMethod(methodName, root);
                }

                var memberName = memberAccess.Name.Identifier.ValueText;
                if (memberName == "ConfigureAwait")
                {
                    return IsTaskLikeExpression(memberAccess.Expression, root);
                }

                if ((memberName is "FromResult" or "Run" or "Delay" or "WhenAll" or "WhenAny") &&
                    IsTaskTypeExpression(memberAccess.Expression))
                {
                    return true;
                }

                return IsDeclaredTaskLikeMethod(memberName, root);
            }

            if (expression is MemberAccessExpressionSyntax propertyAccess)
            {
                if (propertyAccess.Name.Identifier.ValueText == "CompletedTask" &&
                    IsTaskTypeExpression(propertyAccess.Expression))
                {
                    return true;
                }

                if (propertyAccess.Expression is ThisExpressionSyntax)
                {
                    return IsTaskLikeMember(propertyAccess.Name.Identifier.ValueText, expression, root);
                }

                return false;
            }

            if (expression is IdentifierNameSyntax name)
            {
                return IsTaskLikeMember(name.Identifier.ValueText, expression, root);
            }

            return false;
        }

        private static bool IsDeclaredTaskLikeMethod(string? methodName, SyntaxNode root)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return false;
            }

            var declarations = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.ValueText == methodName)
                .ToArray();

            return declarations.Length > 0 && declarations.All(method => IsTaskLikeType(method.ReturnType));
        }

        private static bool IsTaskLikeMember(string memberName, ExpressionSyntax expression, SyntaxNode root)
        {
            var method = expression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method is not null)
            {
                if (method.ParameterList.Parameters.Any(parameter =>
                    parameter.Identifier.ValueText == memberName &&
                    parameter.Type is not null &&
                    IsTaskLikeType(parameter.Type)))
                {
                    return true;
                }

                foreach (var variable in method.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                {
                    if (variable.Identifier.ValueText != memberName || variable.Parent is not VariableDeclarationSyntax declaration)
                    {
                        continue;
                    }

                    if (IsTaskLikeType(declaration.Type))
                    {
                        return true;
                    }

                    if (declaration.Type is IdentifierNameSyntax { Identifier.ValueText: "var" } &&
                        variable.Initializer?.Value is InvocationExpressionSyntax initializer &&
                        IsTaskLikeExpression(initializer, root))
                    {
                        return true;
                    }
                }
            }

            var containingType = expression.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (containingType is null)
            {
                return false;
            }

            foreach (var field in containingType.Members.OfType<FieldDeclarationSyntax>())
            {
                if (IsTaskLikeType(field.Declaration.Type) &&
                    field.Declaration.Variables.Any(variable => variable.Identifier.ValueText == memberName))
                {
                    return true;
                }
            }

            return containingType.Members.OfType<PropertyDeclarationSyntax>().Any(property =>
                property.Identifier.ValueText == memberName && IsTaskLikeType(property.Type));
        }

        private static bool IsTaskLikeType(TypeSyntax type)
        {
            var simpleName = type switch
            {
                GenericNameSyntax genericName => genericName.Identifier.ValueText,
                IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
                AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
                NullableTypeSyntax nullableType => GetSimpleTypeName(nullableType.ElementType),
                _ => null
            };

            return simpleName is "Task" or "ValueTask";
        }

        private static string? GetSimpleTypeName(TypeSyntax type) => type switch
        {
            GenericNameSyntax genericName => genericName.Identifier.ValueText,
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
            _ => null
        };

        private static bool IsTaskTypeExpression(ExpressionSyntax expression) => expression switch
        {
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText is "Task" or "ValueTask",
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText is "Task" or "ValueTask",
            _ => false
        };

        private static bool HasZeroTimeout(InvocationExpressionSyntax invocation)
        {
            if (invocation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var timeout = invocation.ArgumentList.Arguments[0].Expression;
            if (timeout is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                return literal.Token.Value switch
                {
                    int value => value == 0,
                    long value => value == 0,
                    uint value => value == 0,
                    ulong value => value == 0,
                    _ => false
                };
            }

            return timeout is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == "Zero" &&
                memberAccess.Expression.ToString().EndsWith("TimeSpan", StringComparison.Ordinal);
        }

        private static IEnumerable<CodeAnalysisViolation> AnalyzeInsecureStringOperations(SyntaxNode root, string filePath)
        {
            var violations = new List<CodeAnalysisViolation>();

            // Find string concatenation in loops (potential inefficiency)
            var stringConcatInLoops = root.DescendantNodes()
                .Where(n => n is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax)
                .SelectMany(loop => loop.DescendantNodes()
                    .OfType<BinaryExpressionSyntax>()
                    .Where(b => b.OperatorToken.Text == "+" &&
                               (b.Left is IdentifierNameSyntax || b.Right is IdentifierNameSyntax)));

            foreach (var concat in stringConcatInLoops)
            {
                violations.Add(new CodeAnalysisViolation
                {
                    FilePath = filePath,
                    LineNumber = concat.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Rule = "InefficientStringConcatenation",
                    Severity = ViolationSeverity.Warning,
                    Message = "String concatenation in loop detected",
                    CodeSnippet = concat.ToString(),
                    Recommendation = "Use StringBuilder for string concatenation in loops"
                });
            }

            return violations;
        }

        private static IEnumerable<CodeAnalysisViolation> AnalyzeExceptionHandling(SyntaxNode root, string filePath)
        {
            var violations = new List<CodeAnalysisViolation>();

            // Find empty catch blocks
            var catchClauses = root.DescendantNodes().OfType<CatchClauseSyntax>();

            foreach (var catchClause in catchClauses)
            {
                var block = catchClause.Block;
                // A filter is an explicit condition, so this is not an unconditional swallow.
                if (block?.Statements.Count == 0 && catchClause.Filter is null)
                {
                    violations.Add(new CodeAnalysisViolation
                    {
                        FilePath = filePath,
                        LineNumber = catchClause.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Rule = "EmptyCatchBlock",
                        Severity = ViolationSeverity.Warning,
                        Message = "Empty catch block swallows exceptions",
                        CodeSnippet = catchClause.ToString(),
                        Recommendation = "Add exception handling logic or remove empty catch block"
                    });
                }
            }

            // Find catch blocks that only log and rethrow (could be simplified)
            var loggingCatches = root.DescendantNodes().OfType<CatchClauseSyntax>()
                .Where(c => c.Block?.Statements.Count == 1 &&
                           c.Block.Statements[0] is ExpressionStatementSyntax expr &&
                           expr.Expression is InvocationExpressionSyntax invocation &&
                           invocation.Expression is MemberAccessExpressionSyntax member &&
                           (member.Name.Identifier.Text.Contains("Log") ||
                            member.Name.Identifier.Text.Contains("Write")));

            foreach (var catchClause in loggingCatches)
            {
                violations.Add(new CodeAnalysisViolation
                {
                    FilePath = filePath,
                    LineNumber = catchClause.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Rule = "LoggingOnlyCatch",
                    Severity = ViolationSeverity.Info,
                    Message = "Catch block only logs and may rethrow",
                    CodeSnippet = catchClause.ToString(),
                    Recommendation = "Consider removing catch block if only logging, or add handling logic"
                });
            }

            return violations;
        }

        private static IEnumerable<CodeAnalysisViolation> AnalyzeSecurityIssues(SyntaxNode root, string filePath)
        {
            var violations = new List<CodeAnalysisViolation>();

            // Find potential SQL injection vulnerabilities
            var stringInterpolations = root.DescendantNodes()
                .OfType<InterpolatedStringExpressionSyntax>()
                .Where(i => i.ToString().Contains("SELECT", StringComparison.OrdinalIgnoreCase) ||
                           i.ToString().Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                           i.ToString().Contains("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                           i.ToString().Contains("DELETE", StringComparison.OrdinalIgnoreCase));

            foreach (var interpolation in stringInterpolations)
            {
                violations.Add(new CodeAnalysisViolation
                {
                    FilePath = filePath,
                    LineNumber = interpolation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Rule = "PotentialSqlInjection",
                    Severity = ViolationSeverity.Error,
                    Message = "String interpolation with SQL keywords detected",
                    CodeSnippet = interpolation.ToString(),
                    Recommendation = "Use parameterized queries instead of string interpolation for SQL"
                });
            }

            // Find use of dangerous APIs
            var dangerousInvocations = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression is MemberAccessExpressionSyntax m &&
                           (m.Name.Identifier.Text == "ExecuteSqlRaw" ||
                            m.Name.Identifier.Text == "FromSqlRaw" ||
                            m.Name.Identifier.Text == "ProcessStart" ||
                            m.Name.Identifier.Text == "ExecuteCommand"));

            foreach (var invocation in dangerousInvocations)
            {
                violations.Add(new CodeAnalysisViolation
                {
                    FilePath = filePath,
                    LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Rule = "DangerousApiUsage",
                    Severity = ViolationSeverity.Warning,
                    Message = $"Potentially dangerous API usage: {invocation.Expression}",
                    CodeSnippet = invocation.ToString(),
                    Recommendation = "Review usage for security implications and consider safer alternatives"
                });
            }

            return violations;
        }

    }

    /// <summary>
    ///     Represents a code analysis violation found during build-time analysis.
    /// </summary>
    public sealed class CodeAnalysisViolation
    {
        /// <summary>
        ///     Gets the file path where the violation was found.
        /// </summary>
        public string? FilePath { get; init; }

        /// <summary>
        ///     Gets the line number where the violation was found.
        /// </summary>
        public int LineNumber { get; init; }

        /// <summary>
        ///     Gets the rule that was violated.
        /// </summary>
        public string? Rule { get; init; }

        /// <summary>
        ///     Gets the severity of the violation.
        /// </summary>
        public ViolationSeverity Severity { get; init; }

        /// <summary>
        ///     Gets the violation message.
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        ///     Gets the code snippet that caused the violation.
        /// </summary>
        public string? CodeSnippet { get; init; }

        /// <summary>
        ///     Gets the recommended fix.
        /// </summary>
        public string? Recommendation { get; init; }
    }
}
