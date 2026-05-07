// <copyright file="TaintToStreamPositionAnalyzerTests.cs" company="slskdN Team">
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

    public class TaintToStreamPositionAnalyzerTests
    {
        private const string ReaderHarness = @"
namespace Soulseek.Messaging
{
    using System;

    internal sealed class MessageReader<T> where T : Enum
    {
        public int ReadInteger() => 0;
        public long ReadLong() => 0L;
        public string ReadString() => string.Empty;
        public void Seek(int position) { }
        public int Position { get; set; }
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
        public async Task Seek_From_ReadInteger_Reports_CSL0003()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class BadSeek
    {
        public void Parse(MessageReader<DummyCode> reader)
        {
            var offset = reader.ReadInteger();
            reader.Seek(offset);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0003", diagnostics[0].Id);
        }

        [Fact]
        public async Task Enumerable_Skip_From_ReadLong_Reports_CSL0003()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Linq;
    using Soulseek.Messaging;

    internal sealed class BadSkip
    {
        public byte[] Parse(MessageReader<DummyCode> reader, byte[] bytes)
        {
            var offset = (int)reader.ReadLong();
            return bytes.Skip(offset).ToArray();
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0003", diagnostics[0].Id);
        }

        [Fact]
        public async Task Position_Assignment_From_ReadString_Length_Reports_CSL0003()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class BadPosition
    {
        public void Parse(MessageReader<DummyCode> reader)
        {
            var offset = reader.ReadString().Length;
            reader.Position = offset;
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0003", diagnostics[0].Id);
        }

        [Fact]
        public async Task Seek_From_Validated_Count_Does_Not_Report()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class GoodSeek
    {
        public void Parse(MessageReader<DummyCode> reader)
        {
            var offset = ProtocolCountReader.ReadValidatedCount(reader, 1024);
            reader.Seek(offset);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Seek_From_Parameter_Does_Not_Report()
        {
            var source = @"
namespace Probe
{
    using System.IO;

    internal sealed class ParamSeek
    {
        public void Parse(Stream stream, int offset) => stream.Seek(offset, SeekOrigin.Begin);
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
                assemblyName: "TaintToStreamPositionProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new TaintToStreamPositionAnalyzer();
            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == TaintToStreamPositionAnalyzer.DiagnosticId).ToImmutableArray();
        }
    }
}
