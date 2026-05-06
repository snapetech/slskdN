// <copyright file="TaintToAllocationAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0001 — Network-derived allocation size without a sanctioned validator.
    /// </summary>
    /// <remarks>
    ///     Flags allocations whose size expression transitively (intra-procedurally) depends on a value
    ///     read from an untrusted protocol stream (e.g. <c>MessageReader.ReadInteger</c>,
    ///     <c>MessageReader.ReadString().Length</c>) without passing through a sanctioned validator.
    ///     This is the highest-severity council lens
    ///     because a missing guard here is a remote denial-of-service.
    ///
    ///     See <c>docs/dev/bug-council-roslyn-analyzers.md</c> for how to add new lenses.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToAllocationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0001";

        private static readonly LocalizableString Title =
            "Network-derived allocation size lacks a sanctioned validator";

        private static readonly LocalizableString MessageFormat =
            "Allocation size derives from untrusted protocol read '{0}' without passing through a sanctioned validator (e.g. ProtocolCountReader.ReadValidatedCount). " +
            "This is a remote denial-of-service primitive; route the value through a validator before allocating.";

        private static readonly LocalizableString Description =
            "Council taint-to-allocation lens (CSL0001). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> CapacityTypeNames = ImmutableHashSet.Create(
            "List",
            "Dictionary",
            "HashSet",
            "MemoryStream",
            "StringBuilder");

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

            context.RegisterSyntaxNodeAction(AnalyzeArrayCreation, SyntaxKind.ArrayCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeArrayCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ArrayCreationExpressionSyntax)context.Node;
            var rankSpecs = creation.Type?.RankSpecifiers;
            if (rankSpecs == null)
            {
                return;
            }

            foreach (var rank in rankSpecs)
            {
                foreach (var size in rank.Sizes)
                {
                    if (size is OmittedArraySizeExpressionSyntax)
                    {
                        continue;
                    }

                    ReportIfTainted(context, size);
                }
            }
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
            var typeName = symbol?.ContainingType?.Name;
            if (typeName == null || !CapacityTypeNames.Contains(typeName))
            {
                return;
            }

            if (creation.ArgumentList?.Arguments.Count > 0)
            {
                ReportIfTainted(context, creation.ArgumentList.Arguments[0].Expression);
            }
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (symbol?.ContainingType?.Name != "Array" || symbol.Name != "CreateInstance")
            {
                return;
            }

            for (var i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                ReportIfTainted(context, invocation.ArgumentList.Arguments[i].Expression);
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
