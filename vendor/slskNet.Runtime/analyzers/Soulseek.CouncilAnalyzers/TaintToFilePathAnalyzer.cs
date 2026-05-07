// <copyright file="TaintToFilePathAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0004 - Network-derived file path without a sanctioned containment validator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToFilePathAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0004";

        private static readonly LocalizableString Title =
            "Network-derived file path lacks a sanctioned containment validator";

        private static readonly LocalizableString MessageFormat =
            "File path derives from untrusted protocol read '{0}' without passing through a sanctioned path validator. " +
            "A hostile path can escape the intended root or target an unintended filesystem location.";

        private static readonly LocalizableString Description =
            "Council taint-to-file-path lens (CSL0004). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> FileMethodNames = ImmutableHashSet.Create(
            "AppendAllLines",
            "AppendAllText",
            "Copy",
            "Create",
            "CreateText",
            "Delete",
            "Exists",
            "Move",
            "Open",
            "OpenRead",
            "OpenText",
            "OpenWrite",
            "ReadAllBytes",
            "ReadAllLines",
            "ReadAllText",
            "ReadLines",
            "Replace",
            "WriteAllBytes",
            "WriteAllLines",
            "WriteAllText");

        private static readonly ImmutableHashSet<string> DirectoryMethodNames = ImmutableHashSet.Create(
            "CreateDirectory",
            "Delete",
            "EnumerateDirectories",
            "EnumerateFiles",
            "EnumerateFileSystemEntries",
            "Exists",
            "GetDirectories",
            "GetFiles",
            "GetFileSystemEntries",
            "Move");

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
            if (typeName == "File" && FileMethodNames.Contains(symbol.Name))
            {
                ReportPathArguments(context, invocation.ArgumentList, 0, symbol.Name == "Replace" ? 3 : 2);
                return;
            }

            if (typeName == "Directory" && DirectoryMethodNames.Contains(symbol.Name))
            {
                ReportPathArguments(context, invocation.ArgumentList, 0, symbol.Name == "Move" ? 2 : 1);
            }
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
            var typeName = symbol?.ContainingType?.Name;
            if (typeName != "FileInfo" && typeName != "DirectoryInfo" && typeName != "FileStream")
            {
                return;
            }

            if (creation.ArgumentList != null)
            {
                ReportPathArguments(context, creation.ArgumentList, 0, 1);
            }
        }

        private static void ReportPathArguments(
            SyntaxNodeAnalysisContext context,
            BaseArgumentListSyntax argumentList,
            int startIndex,
            int maxCount)
        {
            var reported = 0;
            for (var index = startIndex; index < argumentList.Arguments.Count && reported < maxCount; index++)
            {
                ReportIfTainted(context, argumentList.Arguments[index].Expression);
                reported++;
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
