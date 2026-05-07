// <copyright file="TaintToEnumAnalyzer.cs" company="slskdN Team">
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.CouncilAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    ///     CSL0007 - Network-derived enum/status conversion without defined-value validation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToEnumAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0007";

        private static readonly LocalizableString Title =
            "Network-derived enum conversion lacks defined-value validation";

        private static readonly LocalizableString MessageFormat =
            "Enum or status value derives from untrusted protocol read '{0}' without passing through a sanctioned defined-value validator. " +
            "Undefined protocol states can bypass switch handling or capability checks.";

        private static readonly LocalizableString Description =
            "Council taint-to-enum lens (CSL0007). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
            {
                return;
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeCast, SyntaxKind.CastExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeCast(SyntaxNodeAnalysisContext context)
        {
            var cast = (CastExpressionSyntax)context.Node;
            var targetType = context.SemanticModel.GetTypeInfo(cast.Type).Type;
            if (targetType?.TypeKind == TypeKind.Enum && !IsMessageCodeEnum(targetType) && !IsImmediateValidatorArgument(context, cast))
            {
                ReportIfTainted(context, cast.Expression);
            }
        }

        private static bool IsMessageCodeEnum(ITypeSymbol targetType)
        {
            return targetType.ContainingType?.Name == "MessageCode";
        }

        private static bool IsImmediateValidatorArgument(SyntaxNodeAnalysisContext context, CastExpressionSyntax cast)
        {
            for (SyntaxNode? node = cast.Parent; node != null; node = node.Parent)
            {
                if (node is InvocationExpressionSyntax invocation)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    return symbol?.Name == "ValidateDefinedEnum" ||
                        invocation.Expression.ToString().Contains("ValidateDefinedEnum");
                }
            }

            return false;
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var argumentList = invocation.ArgumentList;
            if (symbol?.ContainingType?.Name != "Enum" || argumentList == null || argumentList.Arguments.Count == 0)
            {
                return;
            }

            if (symbol.Name == "Parse" || symbol.Name == "TryParse")
            {
                var index = argumentList.Arguments.Count > 1 ? 1 : 0;
                ReportIfTainted(context, argumentList.Arguments[index].Expression);
            }
            else if (symbol.Name == "ToObject" && argumentList.Arguments.Count > 1)
            {
                ReportIfTainted(context, argumentList.Arguments[1].Expression);
            }
        }

        private static void ReportIfTainted(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            var classification = ProtocolTaintAnalysis.ClassifyExpression(context.SemanticModel, expression);
            if (classification.IsTainted && !classification.HasSanctionedValidator)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    expression.GetLocation(),
                    classification.TaintedSourceName ?? "protocol reader"));
            }
        }
    }
}
