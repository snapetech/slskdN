// <copyright file="TaintToMessageBuilderAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0010 - Network-derived outbound message value without outbound argument validation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToMessageBuilderAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0010";

        private static readonly LocalizableString Title =
            "Network-derived outbound message value lacks argument validation";

        private static readonly LocalizableString MessageFormat =
            "Outbound MessageBuilder value derives from untrusted protocol read '{0}' without a sanctioned outbound validator. " +
            "Relayed protocol values should be bounded before they are emitted to peers or the server.";

        private static readonly LocalizableString Description =
            "Council taint-to-message-builder lens (CSL0010). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> BuilderMethodNames = ImmutableHashSet.Create(
            "WriteByte",
            "WriteBytes",
            "WriteCode",
            "WriteInteger",
            "WriteLong",
            "WriteString");

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
            if (symbol?.ContainingType?.Name != "MessageBuilder" || !BuilderMethodNames.Contains(symbol.Name))
            {
                return;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (symbol.Name == "WriteCode" && IsMessageCodeEnum(context, argument.Expression))
                {
                    continue;
                }

                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, argument.Expression);
            }
        }

        private static bool IsMessageCodeEnum(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            var type = context.SemanticModel.GetTypeInfo(expression).Type;
            return type?.TypeKind == TypeKind.Enum && type.ContainingType?.Name == "MessageCode";
        }
    }
}
