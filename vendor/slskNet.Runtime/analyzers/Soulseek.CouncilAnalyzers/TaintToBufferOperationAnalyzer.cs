// <copyright file="TaintToBufferOperationAnalyzer.cs" company="slskdN Team">
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.CouncilAnalyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    ///     CSL0016 - Network-derived buffer/read/write count without a sanctioned bound.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToBufferOperationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0016";

        private static readonly LocalizableString Title =
            "Network-derived buffer operation count lacks a sanctioned bound";

        private static readonly LocalizableString MessageFormat =
            "Buffer, stream, pool, or compression operation count derives from untrusted protocol read '{0}' without a sanctioned bound";

        private static readonly LocalizableString Description =
            "Council taint-to-buffer-operation lens (CSL0016). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> BufferMethodNames = ImmutableHashSet.Create(
            "CopyTo",
            "CopyToAsync",
            "Read",
            "ReadAsync",
            "Rent",
            "Return",
            "Write",
            "WriteAsync");

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
            if (symbol == null || !BufferMethodNames.Contains(symbol.Name))
            {
                return;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (IsBufferCountArgument(symbol, argument))
                {
                    TaintDiagnosticHelpers.ReportIfTainted(context, Rule, argument.Expression);
                }
            }
        }

        private static bool IsBufferCountArgument(IMethodSymbol symbol, ArgumentSyntax argument)
        {
            IParameterSymbol? parameter = null;
            if (argument.NameColon == null && argument.Parent is ArgumentListSyntax argumentList)
            {
                var index = argumentList.Arguments.IndexOf(argument);
                if (index >= 0 && symbol.Parameters.Length > index)
                {
                    parameter = symbol.Parameters[index];
                }
            }
            else if (argument.NameColon != null)
            {
                parameter = symbol.Parameters.FirstOrDefault(p => p.Name == argument.NameColon.Name.Identifier.ValueText);
            }

            var name = parameter?.Name ?? string.Empty;
            return name == "count" ||
                name == "bufferSize" ||
                name == "minimumLength" ||
                name == "length" ||
                name == "size";
        }
    }
}
