// <copyright file="AdditionalSemanticAnalyzerTests.cs" company="slskdN Team">
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

    public class AdditionalSemanticAnalyzerTests
    {
        private const string Harness = @"
namespace Soulseek.Messaging
{
    using System;

    internal sealed class MessageReader<T> where T : Enum
    {
        public byte[] ReadBytes(int count) => new byte[count];
        public int ReadInteger() => 0;
        public string ReadString() => string.Empty;
    }

    internal sealed class MessageBuilder
    {
        public MessageBuilder WriteInteger(int value) => this;
        public MessageBuilder WriteCode<T>(T value) where T : Enum => this;
        public MessageBuilder WriteString(string value) => this;
    }

    internal static class MessageCode
    {
        internal enum Distributed
        {
            None = 0,
        }
    }
}

namespace Soulseek.Messaging.Messages.Server
{
    using System;
    using Soulseek.Messaging;

    internal static class ProtocolValueValidator
    {
        public static int RequireBoundedCapacity(int value) => value;
        public static int RequireBufferCount(int value) => value;
        public static byte[] RequireCryptoMaterial(byte[] value) => value;
        public static string RequireOutboundString(string value) => value;
        public static string RequireSafeProcessArgument(string value) => value;
        public static string ValidateParserLimits(string value) => value;
        public static string NormalizeCacheKey(string value) => value;
        public static string ToDiagnosticString(string value) => value;
    }

    internal enum DummyCode { None }

    internal static class Logger
    {
        public static void WriteLine(string value) { }
    }

    internal static class Regex
    {
        public static string Replace(string input, string pattern, string replacement) => input;
    }

    internal static class Channel
    {
        public static object CreateBounded(int capacity) => new object();
    }
}
";

        [Fact]
        public async Task While_Bound_From_ReadInteger_Reports_CSL0002()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public int Parse(MessageReader<DummyCode> reader)
        {
            var count = reader.ReadInteger();
            var i = 0;
            while (i < count)
            {
                i++;
            }

            return i;
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToLoopBoundAnalyzer(), TaintToLoopBoundAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Diagnostic_Text_From_ReadString_Reports_CSL0009()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public void Parse(MessageReader<DummyCode> reader)
        {
            var text = reader.ReadString();
            Logger.WriteLine(text);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToDiagnosticAnalyzer(), TaintToDiagnosticAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Diagnostic_Text_With_Log_Line_Validator_Does_Not_Report()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public void Parse(MessageReader<DummyCode> reader)
        {
            var text = ProtocolValueValidator.ToDiagnosticString(reader.ReadString());
            Logger.WriteLine(text);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToDiagnosticAnalyzer(), TaintToDiagnosticAnalyzer.DiagnosticId);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Outbound_MessageBuilder_From_ReadString_Reports_CSL0010()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public void Parse(MessageReader<DummyCode> reader, MessageBuilder builder)
        {
            var text = reader.ReadString();
            builder.WriteString(text);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToMessageBuilderAnalyzer(), TaintToMessageBuilderAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Outbound_MessageCode_Dispatch_From_ReadByte_Does_Not_Report_CSL0010()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public void Parse(MessageReader<DummyCode> reader, MessageBuilder builder)
        {
            var code = (MessageCode.Distributed)reader.ReadInteger();
            builder.WriteCode(code);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToMessageBuilderAnalyzer(), TaintToMessageBuilderAnalyzer.DiagnosticId);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Cache_Key_From_ReadString_Reports_CSL0011()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Collections.Generic;
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public void Parse(MessageReader<DummyCode> reader)
        {
            var key = reader.ReadString();
            var cache = new Dictionary<string, int>();
            cache[key] = 1;
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToCacheKeyAnalyzer(), TaintToCacheKeyAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Crypto_Trust_Material_From_ReadBytes_Reports_CSL0012()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Verifier
    {
        public bool VerifySignature(byte[] signature) => true;
    }

    internal sealed class Probe
    {
        public bool Parse(MessageReader<DummyCode> reader, Verifier verifier)
        {
            var signature = reader.ReadBytes(64);
            return verifier.VerifySignature(signature);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToCryptoTrustAnalyzer(), TaintToCryptoTrustAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Dynamic_Execution_Input_From_ReadString_Reports_CSL0013()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System;
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public Type? Parse(MessageReader<DummyCode> reader)
        {
            var typeName = reader.ReadString();
            return Type.GetType(typeName);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToDynamicExecutionAnalyzer(), TaintToDynamicExecutionAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Parser_Runtime_Input_From_ReadString_Reports_CSL0014()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public string Parse(MessageReader<DummyCode> reader)
        {
            var pattern = reader.ReadString();
            return Regex.Replace(""input"", pattern, ""replacement"");
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToParserRuntimeAnalyzer(), TaintToParserRuntimeAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Resource_Capacity_From_ReadInteger_Reports_CSL0015()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public object Parse(MessageReader<DummyCode> reader)
        {
            var count = reader.ReadInteger();
            return Channel.CreateBounded(count);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToResourceCapacityAnalyzer(), TaintToResourceCapacityAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task Buffer_Operation_Count_From_ReadInteger_Reports_CSL0016()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Buffers;
    using Soulseek.Messaging;

    internal sealed class Probe
    {
        public byte[] Parse(MessageReader<DummyCode> reader)
        {
            var count = reader.ReadInteger();
            return ArrayPool<byte>.Shared.Rent(count);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source, new TaintToBufferOperationAnalyzer(), TaintToBufferOperationAnalyzer.DiagnosticId);
            Assert.Single(diagnostics);
        }

        private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
            string source,
            DiagnosticAnalyzer analyzer,
            string diagnosticId)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: "AdditionalSemanticAnalyzerProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == diagnosticId).ToImmutableArray();
        }
    }
}
