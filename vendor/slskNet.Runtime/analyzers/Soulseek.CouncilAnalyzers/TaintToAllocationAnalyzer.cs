// <copyright file="TaintToAllocationAnalyzer.cs" company="slskdN Team">
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.CouncilAnalyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
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
    ///     <c>MessageReader.ReadLong</c>) without passing through a sanctioned validator
    ///     (<c>ProtocolCountReader.ReadValidatedCount</c>, <c>ProtocolValueValidator.*</c>,
    ///     <c>MessageFrameValidator.Validate*</c>). This is the highest-severity council lens
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

        // Method names on a wire-reader type that produce attacker-controlled scalars. The receiver
        // type is filtered separately to avoid flagging local helpers that happen to share names.
        private static readonly ImmutableHashSet<string> TaintedReaderMethodNames = ImmutableHashSet.Create(
            "ReadInteger",
            "ReadLong");

        // Container types whose members are considered untrusted-source readers. Match by simple
        // type name (without generic arity) so MessageReader<T> matches as "MessageReader".
        private static readonly ImmutableHashSet<string> TaintedReaderTypeNames = ImmutableHashSet.Create(
            "MessageReader");

        // Methods whose presence on the dataflow path neutralizes the taint. These are the
        // sanctioned validators declared in docs/dev/bug-council-negative-space.md and the
        // documented helpers in src/Messaging.
        private static readonly ImmutableHashSet<string> SanctionedValidatorMethodNames = ImmutableHashSet.Create(
            "ReadValidatedCount",
            "ValidateNonNegative",
            "ValidateNonNegativeCount",
            "ValidateMatchingCount",
            "ValidateBooleanFlag",
            "ValidateDefinedEnum",
            "ValidatePort",
            "ValidateAdvertisedPort",
            "ValidateMessageLength",
            "ValidateInitMessageLength");

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

                    var classification = ClassifyExpression(context.SemanticModel, size, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
                    if (classification.IsTainted && !classification.HasSanctionedValidator)
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            size.GetLocation(),
                            classification.TaintedSourceName ?? "ReadInteger");
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static Classification ClassifyExpression(
            SemanticModel model,
            ExpressionSyntax expression,
            HashSet<ISymbol> visited)
        {
            if (expression == null)
            {
                return Classification.Clean;
            }

            switch (expression)
            {
                case ParenthesizedExpressionSyntax paren:
                    return ClassifyExpression(model, paren.Expression, visited);

                case CheckedExpressionSyntax check:
                    return ClassifyExpression(model, check.Expression, visited);

                case CastExpressionSyntax cast:
                    return ClassifyExpression(model, cast.Expression, visited);

                case BinaryExpressionSyntax bin:
                    {
                        var left = ClassifyExpression(model, bin.Left, visited);
                        var right = ClassifyExpression(model, bin.Right, visited);
                        return Classification.Combine(left, right);
                    }

                case PrefixUnaryExpressionSyntax pre:
                    return ClassifyExpression(model, pre.Operand, visited);

                case PostfixUnaryExpressionSyntax post:
                    return ClassifyExpression(model, post.Operand, visited);

                case ConditionalExpressionSyntax cond:
                    {
                        var t = ClassifyExpression(model, cond.WhenTrue, visited);
                        var f = ClassifyExpression(model, cond.WhenFalse, visited);
                        return Classification.Combine(t, f);
                    }

                case InvocationExpressionSyntax invocation:
                    return ClassifyInvocation(model, invocation, visited);

                case MemberAccessExpressionSyntax member:
                    {
                        // e.g. `payload.Length` where payload type is bounded; treat as clean unless arg is tainted.
                        return ClassifyExpression(model, member.Expression, visited);
                    }

                case IdentifierNameSyntax identifier:
                    return ClassifyIdentifier(model, identifier, visited);
            }

            return Classification.Clean;
        }

        private static Classification ClassifyInvocation(
            SemanticModel model,
            InvocationExpressionSyntax invocation,
            HashSet<ISymbol> visited)
        {
            var symbolInfo = model.GetSymbolInfo(invocation);
            var symbol = symbolInfo.Symbol as IMethodSymbol;
            var nameForReport = invocation.ToString();

            if (symbol != null)
            {
                nameForReport = symbol.Name;

                // Sanctioned validators short-circuit: their result is treated as clean even if their
                // arguments are tainted. We do still descend into nested invocations *inside* the
                // arguments so that a missing validator deeper down still surfaces.
                if (SanctionedValidatorMethodNames.Contains(symbol.Name))
                {
                    var inner = ClassifyArguments(model, invocation, visited);
                    return new Classification(
                        isTainted: inner.IsTainted,
                        hasSanctionedValidator: true,
                        taintedSourceName: inner.TaintedSourceName);
                }

                // Tainted source: a wire-reader invocation that produces an attacker-controlled scalar.
                if (TaintedReaderMethodNames.Contains(symbol.Name))
                {
                    var receiver = symbol.ContainingType;
                    if (receiver != null && IsTaintedReaderType(receiver))
                    {
                        return new Classification(
                            isTainted: true,
                            hasSanctionedValidator: false,
                            taintedSourceName: $"{receiver.Name}.{symbol.Name}");
                    }
                }
            }

            // Fallthrough: combine classifications of all arguments and the receiver expression.
            var combined = ClassifyArguments(model, invocation, visited);
            if (invocation.Expression is MemberAccessExpressionSyntax ma)
            {
                combined = Classification.Combine(combined, ClassifyExpression(model, ma.Expression, visited));
            }

            return combined;
        }

        private static Classification ClassifyArguments(
            SemanticModel model,
            InvocationExpressionSyntax invocation,
            HashSet<ISymbol> visited)
        {
            var combined = Classification.Clean;
            if (invocation.ArgumentList == null)
            {
                return combined;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                combined = Classification.Combine(combined, ClassifyExpression(model, argument.Expression, visited));
            }

            return combined;
        }

        private static Classification ClassifyIdentifier(
            SemanticModel model,
            IdentifierNameSyntax identifier,
            HashSet<ISymbol> visited)
        {
            var symbol = model.GetSymbolInfo(identifier).Symbol;
            if (symbol == null)
            {
                return Classification.Clean;
            }

            // A parameter is treated as clean here — taint must be local for this lens. Inter-procedural
            // taint is intentionally out of scope; the goal is to catch the high-severity in-method
            // shape (ReadInteger() flowing into new T[N]) without false positives across method boundaries.
            if (symbol is IParameterSymbol)
            {
                return Classification.Clean;
            }

            if (!visited.Add(symbol))
            {
                return Classification.Clean;
            }

            try
            {
                if (symbol is ILocalSymbol local)
                {
                    return ClassifyLocalSymbol(model, local, visited);
                }

                if (symbol is IFieldSymbol)
                {
                    return Classification.Clean;
                }

                if (symbol is IPropertySymbol)
                {
                    return Classification.Clean;
                }
            }
            finally
            {
                visited.Remove(symbol);
            }

            return Classification.Clean;
        }

        private static Classification ClassifyLocalSymbol(
            SemanticModel model,
            ILocalSymbol local,
            HashSet<ISymbol> visited)
        {
            var combined = Classification.Clean;

            foreach (var reference in local.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is VariableDeclaratorSyntax declarator
                    && declarator.Initializer?.Value is ExpressionSyntax init)
                {
                    combined = Classification.Combine(combined, ClassifyExpression(model, init, visited));
                }
            }

            return combined;
        }

        private static bool IsTaintedReaderType(INamedTypeSymbol type)
        {
            for (var t = (INamedTypeSymbol?)type; t != null; t = t.BaseType)
            {
                if (TaintedReaderTypeNames.Contains(t.Name))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct Classification
        {
            public Classification(bool isTainted, bool hasSanctionedValidator, string? taintedSourceName)
            {
                IsTainted = isTainted;
                HasSanctionedValidator = hasSanctionedValidator;
                TaintedSourceName = taintedSourceName;
            }

            public static Classification Clean => default;

            public bool IsTainted { get; }

            public bool HasSanctionedValidator { get; }

            public string? TaintedSourceName { get; }

            public static Classification Combine(Classification a, Classification b)
            {
                return new Classification(
                    isTainted: a.IsTainted || b.IsTainted,
                    hasSanctionedValidator: a.HasSanctionedValidator || b.HasSanctionedValidator,
                    taintedSourceName: a.TaintedSourceName ?? b.TaintedSourceName);
            }
        }
    }
}
