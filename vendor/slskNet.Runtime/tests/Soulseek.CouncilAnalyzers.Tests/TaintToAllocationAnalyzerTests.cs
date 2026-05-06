// <copyright file="TaintToAllocationAnalyzerTests.cs" company="slskdN Team">
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

    public class TaintToAllocationAnalyzerTests
    {
        private const string ReaderHarness = @"
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
        public async Task Allocates_Tainted_ReadInteger_Without_Validator_Reports_CSL0001()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Bad
    {
        public byte[] Parse(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadInteger();
            return new byte[length];
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0001", diagnostics[0].Id);
        }

        [Fact]
        public async Task Allocates_Tainted_ReadInteger_With_Validator_Does_Not_Report()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Good
    {
        public byte[] Parse(MessageReader<DummyCode> reader)
        {
            var length = ProtocolCountReader.ReadValidatedCount(reader, 1024);
            return new byte[length];
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Allocates_Constant_Size_Does_Not_Report()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    internal sealed class ConstantOnly
    {
        public byte[] Parse() => new byte[1024];
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Allocates_Tainted_ReadLong_Through_Cast_Reports_CSL0001()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class CastBad
    {
        public byte[] Parse(MessageReader<DummyCode> reader)
        {
            var length = (int)reader.ReadLong();
            return new byte[length];
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0001", diagnostics[0].Id);
        }

        [Fact]
        public async Task Allocates_Tainted_ReadByte_Reports_CSL0001()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class ByteBad
    {
        public byte[] Parse(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadByte();
            return new byte[length];
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0001", diagnostics[0].Id);
        }

        [Fact]
        public async Task Allocates_Tainted_ReadString_Length_Reports_CSL0001()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class StringLengthBad
    {
        public byte[] Parse(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadString().Length;
            return new byte[length];
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0001", diagnostics[0].Id);
        }

        [Fact]
        public async Task ArrayCreateInstance_Tainted_Length_Reports_CSL0001()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System;
    using Soulseek.Messaging;

    internal sealed class ArrayCreateInstanceBad
    {
        public Array Parse(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadInteger();
            return Array.CreateInstance(typeof(byte), length);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0001", diagnostics[0].Id);
        }

        [Fact]
        public async Task MemoryStream_Tainted_Capacity_Reports_CSL0001()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.IO;
    using Soulseek.Messaging;

    internal sealed class MemoryStreamBad
    {
        public MemoryStream Parse(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadInteger();
            return new MemoryStream(length);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0001", diagnostics[0].Id);
        }

        [Fact]
        public async Task List_Tainted_Capacity_Reports_CSL0001()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Collections.Generic;
    using Soulseek.Messaging;

    internal sealed class ListBad
    {
        public List<byte> Parse(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadInteger();
            return new List<byte>(length);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0001", diagnostics[0].Id);
        }

        [Fact]
        public async Task Allocates_Tainted_Through_Arithmetic_Reports_CSL0001()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Arith
    {
        public byte[] Parse(MessageReader<DummyCode> reader)
        {
            var length = reader.ReadInteger() + 4;
            return new byte[length];
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Allocates_ReadCount_Result_Does_Not_Report()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class ReadCountGood
    {
        public byte[] Parse(MessageReader<DummyCode> reader)
        {
            var length = ProtocolCountReader.ReadCount(reader, ""file"", minimumBytesPerItem: 1);
            return new byte[length];
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Allocates_Parameter_Treated_As_Clean()
        {
            var source = @"
namespace Probe
{
    internal sealed class ParamProbe
    {
        public byte[] Parse(int length) => new byte[length];
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
                assemblyName: "TaintToAllocationProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new TaintToAllocationAnalyzer();
            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == TaintToAllocationAnalyzer.DiagnosticId).ToImmutableArray();
        }
    }
}
