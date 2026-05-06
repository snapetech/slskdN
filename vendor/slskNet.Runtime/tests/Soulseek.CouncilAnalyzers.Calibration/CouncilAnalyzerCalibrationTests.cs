// <copyright file="CouncilAnalyzerCalibrationTests.cs" company="slskdN Team">
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.CouncilAnalyzers.Calibration
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public class CouncilAnalyzerCalibrationTests
    {
        private const string Harness = @"
namespace Soulseek.Messaging
{
    using System;

    internal sealed class MessageReader<T> where T : Enum
    {
        public int ReadByte() => 0;
        public byte[] ReadBytes(int count) => new byte[count];
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
        public async Task Calibration_Corpus_Fires_CSL0001_And_CSL0002_On_Known_Bad_Shapes()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System;
    using System.IO;
    using Soulseek.Messaging;

    internal sealed class KnownBad
    {
        public Array ParseArray(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadInteger();
            return Array.CreateInstance(typeof(byte), length);
        }

        public MemoryStream ParseStream(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadString().Length;
            return new MemoryStream(length);
        }

        public int ParseLoop(MessageReader<DummyCode> reader)
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
            var diagnostics = await RunAnalyzersAsync(source);

            Assert.Equal(2, diagnostics.Count(d => d.Id == TaintToAllocationAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToLoopBoundAnalyzer.DiagnosticId));
        }

        [Fact]
        public async Task Calibration_Corpus_Stays_Silent_On_Sanctioned_Validators()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Collections.Generic;
    using Soulseek.Messaging;

    internal sealed class KnownGood
    {
        public List<byte> ParseList(MessageReader<DummyCode> reader)
        {
            var length = ProtocolCountReader.ReadCount(reader, ""file"", minimumBytesPerItem: 1);
            return new List<byte>(length);
        }

        public int ParseLoop(MessageReader<DummyCode> reader)
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
            var diagnostics = await RunAnalyzersAsync(source);

            Assert.Empty(diagnostics);
        }

        private static async Task<ImmutableArray<Diagnostic>> RunAnalyzersAsync(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: "CouncilAnalyzerCalibrationProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
                new TaintToAllocationAnalyzer(),
                new TaintToLoopBoundAnalyzer());

            var diagnostics = await compilation.WithAnalyzers(analyzers)
                .GetAnalyzerDiagnosticsAsync()
                .ConfigureAwait(false);

            return diagnostics
                .Where(d => d.Id == TaintToAllocationAnalyzer.DiagnosticId || d.Id == TaintToLoopBoundAnalyzer.DiagnosticId)
                .ToImmutableArray();
        }
    }
}
