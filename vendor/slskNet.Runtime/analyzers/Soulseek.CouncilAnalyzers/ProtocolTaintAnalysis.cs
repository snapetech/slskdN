// <copyright file="ProtocolTaintAnalysis.cs" company="slskdN Team">
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.CouncilAnalyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ProtocolTaintAnalysis
    {
        private static readonly ImmutableHashSet<string> TaintedReaderMethodNames = ImmutableHashSet.Create(
            "ReadByte",
            "ReadBytes",
            "ReadCode",
            "ReadInteger",
            "ReadLong",
            "ReadString",
            "ReadStringAndEncoding");

        private static readonly ImmutableHashSet<string> TaintedReaderTypeNames = ImmutableHashSet.Create(
            "MessageReader");

        private static readonly ImmutableHashSet<string> TaintedReaderExtensionTypeNames = ImmutableHashSet.Create(
            "MessageReaderExtensions");

        private static readonly ImmutableHashSet<string> SanctionedValidatorMethodNames = ImmutableHashSet.Create(
            "ReadCount",
            "ReadValidatedCount",
            "ValidateNonNegative",
            "ValidateNonNegativeCount",
            "ValidateMatchingCount",
            "ValidateBooleanFlag",
            "ValidateDefinedEnum",
            "ValidatePort",
            "ValidateAdvertisedPort",
            "ValidateMessageLength",
            "ValidateInitMessageLength",
            "RequireNonNegative",
            "RequireMaximumUtf8Length");

        public static Classification ClassifyExpression(SemanticModel model, ExpressionSyntax? expression)
        {
            return ClassifyExpression(model, expression, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
        }

        private static Classification ClassifyExpression(
            SemanticModel model,
            ExpressionSyntax? expression,
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
                    return Classification.Combine(
                        ClassifyExpression(model, bin.Left, visited),
                        ClassifyExpression(model, bin.Right, visited));

                case AssignmentExpressionSyntax assignment:
                    return ClassifyExpression(model, assignment.Right, visited);

                case PrefixUnaryExpressionSyntax pre:
                    return ClassifyExpression(model, pre.Operand, visited);

                case PostfixUnaryExpressionSyntax post:
                    return ClassifyExpression(model, post.Operand, visited);

                case ConditionalExpressionSyntax cond:
                    return Classification.Combine(
                        ClassifyExpression(model, cond.WhenTrue, visited),
                        ClassifyExpression(model, cond.WhenFalse, visited));

                case InvocationExpressionSyntax invocation:
                    return ClassifyInvocation(model, invocation, visited);

                case MemberAccessExpressionSyntax member:
                    return ClassifyExpression(model, member.Expression, visited);

                case ElementAccessExpressionSyntax element:
                    return Classification.Combine(
                        ClassifyExpression(model, element.Expression, visited),
                        ClassifyArgumentList(model, element.ArgumentList, visited));

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
            var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (symbol != null)
            {
                if (SanctionedValidatorMethodNames.Contains(symbol.Name))
                {
                    var inner = ClassifyArguments(model, invocation, visited);
                    return new Classification(
                        isTainted: inner.IsTainted,
                        hasSanctionedValidator: true,
                        taintedSourceName: inner.TaintedSourceName);
                }

                if (IsTaintedReaderInvocation(symbol, invocation, model))
                {
                    return new Classification(
                        isTainted: true,
                        hasSanctionedValidator: false,
                        taintedSourceName: $"{symbol.ContainingType?.Name ?? "reader"}.{symbol.Name}");
                }
            }

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
            return ClassifyArgumentList(model, invocation.ArgumentList, visited);
        }

        private static Classification ClassifyArgumentList(
            SemanticModel model,
            BaseArgumentListSyntax? argumentList,
            HashSet<ISymbol> visited)
        {
            var combined = Classification.Clean;
            if (argumentList == null)
            {
                return combined;
            }

            foreach (var argument in argumentList.Arguments)
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

        private static bool IsTaintedReaderInvocation(
            IMethodSymbol symbol,
            InvocationExpressionSyntax invocation,
            SemanticModel model)
        {
            if (!TaintedReaderMethodNames.Contains(symbol.Name))
            {
                return false;
            }

            if (symbol.ContainingType != null && IsTaintedReaderType(symbol.ContainingType))
            {
                return true;
            }

            if (symbol.ContainingType == null || !TaintedReaderExtensionTypeNames.Contains(symbol.ContainingType.Name))
            {
                return false;
            }

            if (invocation.Expression is MemberAccessExpressionSyntax ma)
            {
                var receiverType = model.GetTypeInfo(ma.Expression).Type as INamedTypeSymbol;
                return receiverType != null && IsTaintedReaderType(receiverType);
            }

            if (invocation.ArgumentList?.Arguments.Count > 0)
            {
                var firstType = model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression).Type as INamedTypeSymbol;
                return firstType != null && IsTaintedReaderType(firstType);
            }

            return false;
        }

        private static bool IsTaintedReaderType(INamedTypeSymbol type)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                if (TaintedReaderTypeNames.Contains(t.Name))
                {
                    return true;
                }
            }

            return false;
        }

        internal readonly struct Classification
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
