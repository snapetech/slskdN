// <copyright file="TaintToCacheKeyAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0011 - Network-derived cache or dictionary key without normalization/bounding.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToCacheKeyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0011";

        private static readonly LocalizableString Title =
            "Network-derived cache key lacks normalization";

        private static readonly LocalizableString MessageFormat =
            "Cache or dictionary key derives from untrusted protocol read '{0}' without sanctioned key normalization. " +
            "Hostile keys can bypass dedupe, grow caches, or poison wait/correlation maps.";

        private static readonly LocalizableString Description =
            "Council taint-to-cache-key lens (CSL0011). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> KeyMethodNames = ImmutableHashSet.Create(
            "Add",
            "AddOrUpdate",
            "ContainsKey",
            "GetOrAdd",
            "Remove",
            "Set",
            "TryAdd",
            "TryGetValue",
            "TryRemove");

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
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null || !KeyMethodNames.Contains(symbol.Name) || invocation.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            if (!IsDictionaryLike(symbol.ContainingType))
            {
                return;
            }

            TaintDiagnosticHelpers.ReportIfTainted(context, Rule, invocation.ArgumentList.Arguments[0].Expression);
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (assignment.Left is not ElementAccessExpressionSyntax element ||
                element.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            var receiverType = context.SemanticModel.GetTypeInfo(element.Expression).Type as INamedTypeSymbol;
            if (IsDictionaryLike(receiverType))
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, element.ArgumentList.Arguments[0].Expression);
            }
        }

        private static bool IsDictionaryLike(INamedTypeSymbol? type)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                if (t.Name.Contains("Dictionary") || t.Name.Contains("Cache"))
                {
                    return true;
                }
            }

            foreach (var iface in type?.AllInterfaces ?? ImmutableArray<INamedTypeSymbol>.Empty)
            {
                if (iface.Name.Contains("Dictionary"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
