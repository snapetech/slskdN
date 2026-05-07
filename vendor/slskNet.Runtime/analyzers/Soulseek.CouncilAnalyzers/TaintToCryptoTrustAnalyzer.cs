// <copyright file="TaintToCryptoTrustAnalyzer.cs" company="slskdN Team">
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
    ///     CSL0012 - Network-derived cryptographic trust material without size/format verification.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaintToCryptoTrustAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSL0012";

        private static readonly LocalizableString Title =
            "Network-derived cryptographic trust material lacks validation";

        private static readonly LocalizableString MessageFormat =
            "Cryptographic key/signature/trust input derives from untrusted protocol read '{0}' without sanctioned format and length validation";

        private static readonly LocalizableString Description =
            "Council taint-to-crypto-trust lens (CSL0012). See docs/dev/bug-council-roslyn-analyzers.md.";

        private const string Category = "Council.Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly ImmutableHashSet<string> CryptoMethodNames = ImmutableHashSet.Create(
            "DecodePoint",
            "ImportECPrivateKey",
            "ImportEncryptedPkcs8PrivateKey",
            "ImportFromPem",
            "ImportPkcs8PrivateKey",
            "ImportRSAPrivateKey",
            "ImportRSAPublicKey",
            "ImportSubjectPublicKeyInfo",
            "Verify",
            "VerifyData",
            "VerifyHash",
            "VerifySignature");

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
            if (symbol == null || !CryptoMethodNames.Contains(symbol.Name))
            {
                return;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                TaintDiagnosticHelpers.ReportIfTainted(context, Rule, argument.Expression);
            }
        }
    }
}
