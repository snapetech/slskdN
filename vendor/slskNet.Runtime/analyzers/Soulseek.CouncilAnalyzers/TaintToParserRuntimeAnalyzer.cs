// <copyright file="TaintToParserRuntimeAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0014 - Network-derived regex or serializer input without parser limits.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToParserRuntimeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0014";

        private static readonly LocalizableString Title =
            "Network-derived parser runtime input lacks explicit limits";

        private static readonly LocalizableString MessageFormat =
            "Regex/serializer input derives from untrusted protocol read '{0}' without sanctioned parser limits or timeout validation";

        private static readonly LocalizableString Description =
            "Council taint-to-parser-runtime lens (CSL0014). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> ParserMethodNames = ImmutableHashSet.Create(
            "Deserialize",
            "Load",
            "LoadXml",
            "Matches",
            "Parse",
            "Replace",
            "Split");

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
            if (symbol == null || !ParserMethodNames.Contains(symbol.Name) || !IsParserType(symbol.ContainingType))
            {
                return;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, argument.Expression);
            }
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var type = context.SemanticModel.GetTypeInfo(creation.Type).Type;
            if (type?.Name != "Regex")
            {
                return;
            }

            foreach (var argument in creation.ArgumentList?.Arguments ?? default)
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, argument.Expression);
            }
        }

        private static bool IsParserType(INamedTypeSymbol? type)
        {
            if (type == null)
            {
                return false;
            }

            var name = type.Name;
            var containing = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            return name == "Regex" ||
                name == "JsonSerializer" ||
                name == "XmlDocument" ||
                name == "XDocument" ||
                containing.Contains("Newtonsoft.Json");
        }
    }
}
