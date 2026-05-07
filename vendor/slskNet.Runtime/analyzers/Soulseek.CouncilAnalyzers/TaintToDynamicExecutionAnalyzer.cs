// <copyright file="TaintToDynamicExecutionAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0013 - Network-derived reflection or process input without allowlist validation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToDynamicExecutionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0013";

        private static readonly LocalizableString Title =
            "Network-derived dynamic execution input lacks allowlist validation";

        private static readonly LocalizableString MessageFormat =
            "Reflection/process input derives from untrusted protocol read '{0}' without a sanctioned allowlist validator";

        private static readonly LocalizableString Description =
            "Council taint-to-dynamic-execution lens (CSL0013). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> DynamicMethodNames = ImmutableHashSet.Create(
            "CreateInstance",
            "GetMethod",
            "GetProperty",
            "GetType",
            "Load",
            "LoadFrom",
            "Start");

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
            if (symbol == null || !DynamicMethodNames.Contains(symbol.Name) || !IsDynamicExecutionMethod(symbol))
            {
                return;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, argument.Expression);
            }
        }

        private static bool IsDynamicExecutionMethod(IMethodSymbol symbol)
        {
            var containingType = symbol.ContainingType?.Name ?? string.Empty;
            if (symbol.Name == "CreateInstance")
            {
                return containingType == "Activator";
            }

            if (symbol.Name == "Start")
            {
                return containingType == "Process";
            }

            return containingType == "Type" ||
                containingType == "Assembly" ||
                containingType == "Activator";
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var type = context.SemanticModel.GetTypeInfo(creation.Type).Type;
            if (type?.Name != "ProcessStartInfo")
            {
                return;
            }

            foreach (var argument in creation.ArgumentList?.Arguments ?? default)
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, argument.Expression);
            }
        }
    }
}
