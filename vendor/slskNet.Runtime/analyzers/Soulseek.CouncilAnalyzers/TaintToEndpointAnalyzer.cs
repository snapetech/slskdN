// <copyright file="TaintToEndpointAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0006 - Network-derived endpoint or URI component without sanctioned validation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToEndpointAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0006";

        private static readonly LocalizableString Title =
            "Network-derived endpoint lacks sanctioned validation";

        private static readonly LocalizableString MessageFormat =
            "Endpoint, address, or URI component derives from untrusted protocol read '{0}' without passing through a sanctioned validator. " +
            "A hostile endpoint can redirect connections or create invalid network targets.";

        private static readonly LocalizableString Description =
            "Council taint-to-endpoint lens (CSL0006). See docs/dev/bug-council-roslyn-analyzers.md.";

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
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null || invocation.ArgumentList == null)
            {
                return;
            }

            var typeName = symbol.ContainingType?.Name;
            var isEndpointSink =
                (typeName == "IPAddress" && (symbol.Name == "Parse" || symbol.Name == "TryParse")) ||
                (typeName == "Dns" && (symbol.Name == "GetHostEntry" || symbol.Name == "GetHostAddresses")) ||
                (typeName == "Uri" && symbol.Name == "TryCreate");

            if (isEndpointSink && invocation.ArgumentList.Arguments.Count > 0)
            {
                ReportIfTainted(context, invocation.ArgumentList.Arguments[0].Expression);
            }
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
            var typeName = symbol?.ContainingType?.Name;
            if (creation.ArgumentList == null)
            {
                return;
            }

            if (typeName == "IPEndPoint")
            {
                ReportArgument(context, creation.ArgumentList, 0);
                ReportArgument(context, creation.ArgumentList, 1);
            }
            else if (typeName == "Uri")
            {
                ReportArgument(context, creation.ArgumentList, 0);
            }
        }

        private static void ReportArgument(SyntaxNodeAnalysisContext context, BaseArgumentListSyntax arguments, int index)
        {
            if (arguments.Arguments.Count > index)
            {
                ReportIfTainted(context, arguments.Arguments[index].Expression);
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
