// <copyright file="TaintToEndpointAnalyzerTests.cs" company="slskdN Team">
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

    public class TaintToEndpointAnalyzerTests
    {
        private const string Harness = @"
namespace Soulseek.Messaging
{
    using System;

    internal sealed class MessageReader<T> where T : Enum
    {
        public int ReadInteger() => 0;
        public string ReadString() => string.Empty;
    }
}

namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal static class ProtocolValueValidator
    {
        public static int ValidatePort(int value) => value;
        public static string ResolveSafeEndpoint(string value) => value;
    }

    internal enum DummyCode { None }
}
";

        [Fact]
        public async Task IPAddressParse_From_ReadString_Reports_CSL0006()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Net;
    using Soulseek.Messaging;

    internal sealed class BadAddress
    {
        public IPAddress Parse(MessageReader<DummyCode> reader)
        {
            var address = reader.ReadString();
            return IPAddress.Parse(address);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0006", diagnostics[0].Id);
        }

        [Fact]
        public async Task IPEndPoint_From_ReadInteger_Port_Reports_CSL0006()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Net;
    using Soulseek.Messaging;

    internal sealed class BadEndpoint
    {
        public IPEndPoint Parse(MessageReader<DummyCode> reader)
        {
            var port = reader.ReadInteger();
            return new IPEndPoint(IPAddress.Loopback, port);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0006", diagnostics[0].Id);
        }

        [Fact]
        public async Task Uri_From_ReadString_Reports_CSL0006()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System;
    using Soulseek.Messaging;

    internal sealed class BadUri
    {
        public Uri Parse(MessageReader<DummyCode> reader)
        {
            var value = reader.ReadString();
            return new Uri(value);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0006", diagnostics[0].Id);
        }

        [Fact]
        public async Task IPEndPoint_From_Validated_Port_Does_Not_Report()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Net;
    using Soulseek.Messaging;

    internal sealed class GoodEndpoint
    {
        public IPEndPoint Parse(MessageReader<DummyCode> reader)
        {
            var port = ProtocolValueValidator.ValidatePort(reader.ReadInteger());
            return new IPEndPoint(IPAddress.Loopback, port);
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
                assemblyName: "TaintToEndpointProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new TaintToEndpointAnalyzer();
            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == TaintToEndpointAnalyzer.DiagnosticId).ToImmutableArray();
        }
    }
}
