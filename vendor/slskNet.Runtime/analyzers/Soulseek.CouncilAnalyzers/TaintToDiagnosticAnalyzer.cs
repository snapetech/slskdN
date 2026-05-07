// <copyright file="TaintToDiagnosticAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0009 - Network-derived diagnostic text without log-line/control-character validation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0009";

        private static readonly LocalizableString Title =
            "Network-derived diagnostic text lacks log-line validation";

        private static readonly LocalizableString MessageFormat =
            "Diagnostic/log argument derives from untrusted protocol read '{0}' without a sanctioned log-line validator. " +
            "Protocol text should stay visible but must not inject control characters or forged lines.";

        private static readonly LocalizableString Description =
            "Council taint-to-diagnostic lens (CSL0009). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> DiagnosticMethodNames = ImmutableHashSet.Create(
            "Log",
            "Trace",
            "Debug",
            "Info",
            "Information",
            "Warn",
            "Warning",
            "Error",
            "Critical",
            "Write",
            "WriteLine",
            "CreateDiagnostic",
            "DebugDiagnostic",
            "Diagnostic");

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
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null || !DiagnosticMethodNames.Contains(symbol.Name))
            {
                return;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, argument.Expression);
            }
        }
    }
}
