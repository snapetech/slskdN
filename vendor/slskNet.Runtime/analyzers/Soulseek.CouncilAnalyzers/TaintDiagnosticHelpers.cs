// <copyright file="TaintDiagnosticHelpers.cs" company="slskdN Team">
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.CouncilAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    internal static class TaintDiagnosticHelpers
    {
        public static void ReportIfTainted(
            SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor rule,
            ExpressionSyntax expression)
        {
            var classification = ProtocolTaintAnalysis.ClassifyExpression(context.SemanticModel, expression);
            if (classification.IsTainted && !classification.HasSanctionedValidator)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    rule,
                    expression.GetLocation(),
                    classification.TaintedSourceName ?? "protocol reader"));
            }
        }
    }
}
