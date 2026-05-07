// <copyright file="TaintToStreamPositionAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0003 - Network-derived stream position or skip count without a sanctioned validator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToStreamPositionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0003";

        private static readonly LocalizableString Title =
            "Network-derived stream position lacks a sanctioned validator";

        private static readonly LocalizableString MessageFormat =
            "Stream position or skip count derives from untrusted protocol read '{0}' without passing through a sanctioned validator. " +
            "A hostile offset can desynchronize parsing or seek outside the intended frame.";

        private static readonly LocalizableString Description =
            "Council taint-to-stream-position lens (CSL0003). See docs/dev/bug-council-roslyn-analyzers.md.";

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
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null || (symbol.Name != "Seek" && symbol.Name != "Skip"))
            {
                return;
            }

            if (invocation.ArgumentList?.Arguments.Count > 0)
            {
                ReportIfTainted(context, invocation.ArgumentList.Arguments[0].Expression);
            }
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (!IsPositionAssignment(assignment.Left))
            {
                return;
            }

            ReportIfTainted(context, assignment.Right);
        }

        private static bool IsPositionAssignment(ExpressionSyntax left)
        {
            switch (left)
            {
                case IdentifierNameSyntax identifier:
                    return identifier.Identifier.ValueText == "Position";

                case MemberAccessExpressionSyntax member:
                    return member.Name.Identifier.ValueText == "Position";

                default:
                    return false;
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
