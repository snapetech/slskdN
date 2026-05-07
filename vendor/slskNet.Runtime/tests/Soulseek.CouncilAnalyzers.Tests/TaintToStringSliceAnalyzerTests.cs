// <copyright file="TaintToStringSliceAnalyzerTests.cs" company="slskdN Team">
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.CouncilAnalyzers.Tests
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Soulseek.CouncilAnalyzers;
    using Xunit;

    public class TaintToStringSliceAnalyzerTests
    {
        private const string Harness = @"
namespace Soulseek.Messaging
{
    using System;

    internal sealed class MessageReader<T> where T : Enum
    {
        public int ReadInteger() => 0;
    }
}

namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal static class ProtocolValueValidator
    {
        public static int ValidateSliceBounds(int value) => value;
    }

    internal enum DummyCode { None }
}
";

        [Fact]
        public async Task Substring_From_ReadInteger_Reports_CSL0008()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class BadSubstring
    {
        public string Parse(MessageReader<DummyCode> reader, string text)
        {
            var offset = reader.ReadInteger();
            return text.Substring(offset);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0008", diagnostics[0].Id);
        }

        [Fact]
        public async Task ElementAccess_From_ReadInteger_Reports_CSL0008()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class BadIndex
    {
        public char Parse(MessageReader<DummyCode> reader, string text)
        {
            var offset = reader.ReadInteger();
            return text[offset];
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0008", diagnostics[0].Id);
        }

        [Fact]
        public async Task Substring_From_Validated_Bound_Does_Not_Report()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class GoodSubstring
    {
        public string Parse(MessageReader<DummyCode> reader, string text)
        {
            var offset = ProtocolValueValidator.ValidateSliceBounds(reader.ReadInteger());
            return text.Substring(offset);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Empty(diagnostics);
        }

        private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: "TaintToStringSliceProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new TaintToStringSliceAnalyzer();
            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == TaintToStringSliceAnalyzer.DiagnosticId).ToImmutableArray();
        }
    }
}
