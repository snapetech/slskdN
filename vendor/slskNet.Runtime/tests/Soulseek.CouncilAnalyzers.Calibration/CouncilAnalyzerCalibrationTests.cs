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
        public void Seek(int position) { }
    }

    internal sealed class MessageBuilder
    {
        public MessageBuilder WriteString(string value) => this;
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

    internal static class PathSafety
    {
        public static string ResolveContainedPath(string root, string relativePath) => relativePath;
    }

    internal static class ProtocolValueValidator
    {
        public static int ValidatePort(int value) => value;
        public static int ValidateSliceBounds(int value) => value;
        public static int ValidateTimeout(int value) => value;
        public static DummyStatus ValidateDefinedEnum(DummyStatus value) => value;
        public static int RequireBoundedCapacity(int value) => value;
        public static int RequireBufferCount(int value) => value;
        public static byte[] RequireCryptoMaterial(byte[] value) => value;
        public static string NormalizeCacheKey(string value) => value;
        public static string RequireOutboundString(string value) => value;
        public static string RequireSafeProcessArgument(string value) => value;
        public static string ToDiagnosticString(string value) => value;
        public static string ValidateParserLimits(string value) => value;
    }

    internal static class Logger
    {
        public static void WriteLine(string value) { }
    }

    internal sealed class Verifier
    {
        public bool VerifySignature(byte[] signature) => true;
    }

    internal static class Regex
    {
        public static string Replace(string input, string pattern, string replacement) => input;
    }

    internal static class Channel
    {
        public static object CreateBounded(int capacity) => new object();
    }

    internal sealed class Pool
    {
        public byte[] Rent(int minimumLength) => new byte[minimumLength];
    }

    internal enum DummyCode { None }
    internal enum DummyStatus { None, Good }
}
";

        [Fact]
        public async Task Calibration_Corpus_Fires_CSL0001_Through_CSL0016_On_Known_Bad_Shapes()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
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

        public void ParseSeek(MessageReader<DummyCode> reader)
        {
            var offset = reader.ReadInteger();
            reader.Seek(offset);
        }

        public string ParseFile(MessageReader<DummyCode> reader)
        {
            var path = reader.ReadString();
            return File.ReadAllText(path);
        }

        public Task ParseDelay(MessageReader<DummyCode> reader)
        {
            var timeout = reader.ReadInteger();
            return Task.Delay(timeout);
        }

        public IPEndPoint ParseEndpoint(MessageReader<DummyCode> reader)
        {
            var port = reader.ReadInteger();
            return new IPEndPoint(IPAddress.Loopback, port);
        }

        public DummyStatus ParseStatus(MessageReader<DummyCode> reader)
        {
            var status = reader.ReadInteger();
            return (DummyStatus)status;
        }

        public string ParseSlice(MessageReader<DummyCode> reader, string text)
        {
            var offset = reader.ReadInteger();
            return text.Substring(offset);
        }

        public void ParseDiagnostic(MessageReader<DummyCode> reader)
        {
            var text = reader.ReadString();
            Logger.WriteLine(text);
        }

        public void ParseOutbound(MessageReader<DummyCode> reader, MessageBuilder builder)
        {
            var text = reader.ReadString();
            builder.WriteString(text);
        }

        public void ParseCache(MessageReader<DummyCode> reader)
        {
            var key = reader.ReadString();
            var cache = new System.Collections.Generic.Dictionary<string, int>();
            cache[key] = 1;
        }

        public bool ParseCrypto(MessageReader<DummyCode> reader, Verifier verifier)
        {
            var signature = reader.ReadBytes(64);
            return verifier.VerifySignature(signature);
        }

        public Type? ParseDynamic(MessageReader<DummyCode> reader)
        {
            var typeName = reader.ReadString();
            return Type.GetType(typeName);
        }

        public string ParseRuntime(MessageReader<DummyCode> reader)
        {
            var pattern = reader.ReadString();
            return Regex.Replace(""input"", pattern, ""replacement"");
        }

        public object ParseCapacity(MessageReader<DummyCode> reader)
        {
            var capacity = reader.ReadInteger();
            return Channel.CreateBounded(capacity);
        }

        public byte[] ParseBuffer(MessageReader<DummyCode> reader, Pool pool)
        {
            var count = reader.ReadInteger();
            return pool.Rent(count);
        }
    }
}
";
            var diagnostics = await RunAnalyzersAsync(source);

            Assert.Equal(2, diagnostics.Count(d => d.Id == TaintToAllocationAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToLoopBoundAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToStreamPositionAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToFilePathAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToTimeoutAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToEndpointAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToEnumAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToStringSliceAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToDiagnosticAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToMessageBuilderAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToCacheKeyAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToCryptoTrustAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToDynamicExecutionAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToParserRuntimeAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToResourceCapacityAnalyzer.DiagnosticId));
            Assert.Single(diagnostics.Where(d => d.Id == TaintToBufferOperationAnalyzer.DiagnosticId));
        }

        [Fact]
        public async Task Calibration_Corpus_Stays_Silent_On_Sanctioned_Validators()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
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

        public void ParseSeek(MessageReader<DummyCode> reader)
        {
            var offset = ProtocolCountReader.ReadValidatedCount(reader, 1024);
            reader.Seek(offset);
        }

        public string ParseFile(MessageReader<DummyCode> reader, string root)
        {
            var path = PathSafety.ResolveContainedPath(root, reader.ReadString());
            return System.IO.File.ReadAllText(path);
        }

        public Task ParseDelay(MessageReader<DummyCode> reader)
        {
            var timeout = ProtocolValueValidator.ValidateTimeout(reader.ReadInteger());
            return Task.Delay(timeout);
        }

        public IPEndPoint ParseEndpoint(MessageReader<DummyCode> reader)
        {
            var port = ProtocolValueValidator.ValidatePort(reader.ReadInteger());
            return new IPEndPoint(IPAddress.Loopback, port);
        }

        public DummyStatus ParseStatus(MessageReader<DummyCode> reader)
        {
            return ProtocolValueValidator.ValidateDefinedEnum((DummyStatus)reader.ReadInteger());
        }

        public string ParseSlice(MessageReader<DummyCode> reader, string text)
        {
            var offset = ProtocolValueValidator.ValidateSliceBounds(reader.ReadInteger());
            return text.Substring(offset);
        }

        public void ParseDiagnostic(MessageReader<DummyCode> reader)
        {
            var text = ProtocolValueValidator.ToDiagnosticString(reader.ReadString());
            Logger.WriteLine(text);
        }

        public void ParseOutbound(MessageReader<DummyCode> reader, MessageBuilder builder)
        {
            var text = ProtocolValueValidator.RequireOutboundString(reader.ReadString());
            builder.WriteString(text);
        }

        public void ParseCache(MessageReader<DummyCode> reader)
        {
            var key = ProtocolValueValidator.NormalizeCacheKey(reader.ReadString());
            var cache = new System.Collections.Generic.Dictionary<string, int>();
            cache[key] = 1;
        }

        public bool ParseCrypto(MessageReader<DummyCode> reader, Verifier verifier)
        {
            var signature = ProtocolValueValidator.RequireCryptoMaterial(reader.ReadBytes(64));
            return verifier.VerifySignature(signature);
        }

        public Type? ParseDynamic(MessageReader<DummyCode> reader)
        {
            var typeName = ProtocolValueValidator.RequireSafeProcessArgument(reader.ReadString());
            return Type.GetType(typeName);
        }

        public string ParseRuntime(MessageReader<DummyCode> reader)
        {
            var pattern = ProtocolValueValidator.ValidateParserLimits(reader.ReadString());
            return Regex.Replace(""input"", pattern, ""replacement"");
        }

        public object ParseCapacity(MessageReader<DummyCode> reader)
        {
            var capacity = ProtocolValueValidator.RequireBoundedCapacity(reader.ReadInteger());
            return Channel.CreateBounded(capacity);
        }

        public byte[] ParseBuffer(MessageReader<DummyCode> reader, Pool pool)
        {
            var count = ProtocolValueValidator.RequireBufferCount(reader.ReadInteger());
            return pool.Rent(count);
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
                new TaintToLoopBoundAnalyzer(),
                new TaintToStreamPositionAnalyzer(),
                new TaintToFilePathAnalyzer(),
                new TaintToTimeoutAnalyzer(),
                new TaintToEndpointAnalyzer(),
                new TaintToEnumAnalyzer(),
                new TaintToStringSliceAnalyzer(),
                new TaintToDiagnosticAnalyzer(),
                new TaintToMessageBuilderAnalyzer(),
                new TaintToCacheKeyAnalyzer(),
                new TaintToCryptoTrustAnalyzer(),
                new TaintToDynamicExecutionAnalyzer(),
                new TaintToParserRuntimeAnalyzer(),
                new TaintToResourceCapacityAnalyzer(),
                new TaintToBufferOperationAnalyzer());

            var diagnostics = await compilation.WithAnalyzers(analyzers)
                .GetAnalyzerDiagnosticsAsync()
                .ConfigureAwait(false);

            return diagnostics
                .Where(d =>
                    d.Id == TaintToAllocationAnalyzer.DiagnosticId ||
                    d.Id == TaintToLoopBoundAnalyzer.DiagnosticId ||
                    d.Id == TaintToStreamPositionAnalyzer.DiagnosticId ||
                    d.Id == TaintToFilePathAnalyzer.DiagnosticId ||
                    d.Id == TaintToTimeoutAnalyzer.DiagnosticId ||
                    d.Id == TaintToEndpointAnalyzer.DiagnosticId ||
                    d.Id == TaintToEnumAnalyzer.DiagnosticId ||
                    d.Id == TaintToStringSliceAnalyzer.DiagnosticId ||
                    d.Id == TaintToDiagnosticAnalyzer.DiagnosticId ||
                    d.Id == TaintToMessageBuilderAnalyzer.DiagnosticId ||
                    d.Id == TaintToCacheKeyAnalyzer.DiagnosticId ||
                    d.Id == TaintToCryptoTrustAnalyzer.DiagnosticId ||
                    d.Id == TaintToDynamicExecutionAnalyzer.DiagnosticId ||
                    d.Id == TaintToParserRuntimeAnalyzer.DiagnosticId ||
                    d.Id == TaintToResourceCapacityAnalyzer.DiagnosticId ||
                    d.Id == TaintToBufferOperationAnalyzer.DiagnosticId)
                .ToImmutableArray();
        }
    }
}
