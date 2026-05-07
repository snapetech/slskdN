// <copyright file="TaintToTimeoutAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0005 - Network-derived timeout or delay without a sanctioned range validator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToTimeoutAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0005";

        private static readonly LocalizableString Title =
            "Network-derived timeout lacks a sanctioned range validator";

        private static readonly LocalizableString MessageFormat =
            "Timeout or delay derives from untrusted protocol read '{0}' without passing through a sanctioned range validator. " +
            "A hostile duration can disable cancellation, delay work indefinitely, or create tight retry loops.";

        private static readonly LocalizableString Description =
            "Council taint-to-timeout lens (CSL0005). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> DurationFactoryNames = ImmutableHashSet.Create(
            "FromDays",
            "FromHours",
            "FromMilliseconds",
            "FromMinutes",
            "FromSeconds",
            "FromTicks");

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
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var argumentList = invocation.ArgumentList;
            if (symbol == null || argumentList == null || argumentList.Arguments.Count == 0)
            {
                return;
            }

            var typeName = symbol.ContainingType?.Name;
            var isTimeoutSink =
                (typeName == "Task" && symbol.Name == "Delay") ||
                (typeName == "Thread" && symbol.Name == "Sleep") ||
                (typeName == "CancellationTokenSource" && symbol.Name == "CancelAfter") ||
                (typeName == "TimeSpan" && DurationFactoryNames.Contains(symbol.Name));

            if (isTimeoutSink)
            {
                ReportIfTainted(context, argumentList.Arguments[0].Expression);
            }
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
            var typeName = symbol?.ContainingType?.Name;
            var argumentList = creation.ArgumentList;
            if ((typeName != "Timer" && typeName != "CancellationTokenSource") || argumentList == null || argumentList.Arguments.Count == 0)
            {
                return;
            }

            ReportIfTainted(context, argumentList.Arguments[0].Expression);
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
