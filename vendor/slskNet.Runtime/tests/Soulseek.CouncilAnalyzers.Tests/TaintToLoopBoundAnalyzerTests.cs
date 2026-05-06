// <copyright file="TaintToLoopBoundAnalyzerTests.cs" company="slskdN Team">
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

    public class TaintToLoopBoundAnalyzerTests
    {
        private const string ReaderHarness = @"
namespace Soulseek.Messaging
{
    using System;

    internal sealed class MessageReader<T> where T : Enum
    {
        public int ReadByte() => 0;
        public int ReadInteger() => 0;
        public long ReadLong() => 0L;
        public string ReadString() => string.Empty;
    }
}

namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal static class ProtocolCountReader
    {
        public static int ReadValidatedCount(MessageReader<DummyCode> reader, int max) => 0;
        public static int ReadCount(MessageReader<DummyCode> reader, string collectionName, int minimumBytesPerItem) => 0;
    }

    internal enum DummyCode { None }
}
";

        [Fact]
        public async Task Loop_Bound_From_ReadInteger_Reports_CSL0002()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class BadLoop
    {
        public int Parse(MessageReader<DummyCode> reader)
        {
            var count = reader.ReadInteger();
            var total = 0;
            for (var i = 0; i < count; i++)
            {
                total++;
            }

            return total;
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0002", diagnostics[0].Id);
        }

        [Fact]
        public async Task Reversed_Loop_Bound_From_ReadInteger_Reports_CSL0002()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class ReversedBadLoop
    {
        public int Parse(MessageReader<DummyCode> reader)
        {
            var count = reader.ReadInteger();
            var total = 0;
            for (var i = 0; count > i; i++)
            {
                total++;
            }

            return total;
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0002", diagnostics[0].Id);
        }

        [Fact]
        public async Task Loop_Bound_From_ReadString_Length_Reports_CSL0002()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class StringLengthLoop
    {
        public int Parse(MessageReader<DummyCode> reader)
        {
            var count = reader.ReadString().Length;
            var total = 0;
            for (var i = 0; i < count; i++)
            {
                total++;
            }

            return total;
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0002", diagnostics[0].Id);
        }

        [Fact]
        public async Task Loop_Bound_From_Validated_Count_Does_Not_Report()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class GoodLoop
    {
        public int Parse(MessageReader<DummyCode> reader)
        {
            var count = ProtocolCountReader.ReadValidatedCount(reader, 1024);
            var total = 0;
            for (var i = 0; i < count; i++)
            {
                total++;
            }

            return total;
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Loop_Bound_From_Parameter_Does_Not_Report()
        {
            var source = @"
namespace Probe
{
    internal sealed class ParamLoop
    {
        public int Parse(int count)
        {
            var total = 0;
            for (var i = 0; i < count; i++)
            {
                total++;
            }

            return total;
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
                assemblyName: "TaintToLoopBoundProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new TaintToLoopBoundAnalyzer();
            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == TaintToLoopBoundAnalyzer.DiagnosticId).ToImmutableArray();
        }
    }
}
