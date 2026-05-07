// <copyright file="TaintToLoopBoundAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0002 - Network-derived loop bound without a sanctioned validator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToLoopBoundAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0002";

        private static readonly LocalizableString Title =
            "Network-derived loop bound lacks a sanctioned validator";

        private static readonly LocalizableString MessageFormat =
            "Loop bound derives from untrusted protocol read '{0}' without passing through a sanctioned validator. " +
            "A hostile count can become an allocation or CPU denial-of-service through repeated work.";

        private static readonly LocalizableString Description =
            "Council taint-to-loop-bound lens (CSL0002). See docs/dev/bug-council-roslyn-analyzers.md.";

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
            context.RegisterSyntaxNodeAction(AnalyzeForStatement, SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(AnalyzeWhileStatement, SyntaxKind.WhileStatement);
            context.RegisterSyntaxNodeAction(AnalyzeDoStatement, SyntaxKind.DoStatement);
        }

        private static void AnalyzeForStatement(SyntaxNodeAnalysisContext context)
        {
            var statement = (ForStatementSyntax)context.Node;
            AnalyzeCondition(context, statement.Condition);
        }

        private static void AnalyzeWhileStatement(SyntaxNodeAnalysisContext context)
        {
            var statement = (WhileStatementSyntax)context.Node;
            AnalyzeCondition(context, statement.Condition);
        }

        private static void AnalyzeDoStatement(SyntaxNodeAnalysisContext context)
        {
            var statement = (DoStatementSyntax)context.Node;
            AnalyzeCondition(context, statement.Condition);
        }

        private static void AnalyzeCondition(SyntaxNodeAnalysisContext context, ExpressionSyntax? condition)
        {
            if (condition is BinaryExpressionSyntax binary)
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, binary.Left);
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, binary.Right);
                return;
            }

            if (condition != null)
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, condition);
            }
        }
    }
}
