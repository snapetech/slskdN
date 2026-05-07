// <copyright file="TaintToStringSliceAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0008 - Network-derived slice index or length without a sanctioned range validator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToStringSliceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0008";

        private static readonly LocalizableString Title =
            "Network-derived slice bound lacks a sanctioned range validator";

        private static readonly LocalizableString MessageFormat =
            "Slice index or length derives from untrusted protocol read '{0}' without passing through a sanctioned range validator. " +
            "A hostile bound can throw, truncate incorrectly, or desynchronize parser state.";

        private static readonly LocalizableString Description =
            "Council taint-to-string-slice lens (CSL0008). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> SliceMethodNames = ImmutableHashSet.Create(
            "AsMemory",
            "AsSpan",
            "GetRange",
            "Remove",
            "Slice",
            "Substring");

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
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null || !SliceMethodNames.Contains(symbol.Name) || invocation.ArgumentList == null)
            {
                return;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                ReportIfTainted(context, argument.Expression);
            }
        }

        private static void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
        {
            var access = (ElementAccessExpressionSyntax)context.Node;
            if (access.ArgumentList == null)
            {
                return;
            }

            var receiverType = context.SemanticModel.GetTypeInfo(access.Expression).Type;
            if (!IsIndexableSequence(receiverType))
            {
                return;
            }

            foreach (var argument in access.ArgumentList.Arguments)
            {
                ReportIfTainted(context, argument.Expression);
            }
        }

        private static bool IsIndexableSequence(ITypeSymbol? type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.TypeKind == TypeKind.Array ||
                type.SpecialType == SpecialType.System_String ||
                type.Name.Contains("List") ||
                type.Name.Contains("Memory") ||
                type.Name.Contains("Span"))
            {
                return true;
            }

            if (type is not INamedTypeSymbol namedType)
            {
                return false;
            }

            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.Name == "IList" || iface.Name == "IReadOnlyList")
                {
                    return true;
                }
            }

            return false;
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
