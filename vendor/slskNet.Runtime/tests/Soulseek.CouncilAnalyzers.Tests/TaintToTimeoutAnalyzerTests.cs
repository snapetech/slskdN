// <copyright file="TaintToTimeoutAnalyzerTests.cs" company="slskdN Team">
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

    public class TaintToTimeoutAnalyzerTests
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
        public static int ValidateTimeout(int value) => value;
    }

    internal enum DummyCode { None }
}
";

        [Fact]
        public async Task TaskDelay_From_ReadInteger_Reports_CSL0005()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Threading.Tasks;
    using Soulseek.Messaging;

    internal sealed class BadDelay
    {
        public Task Parse(MessageReader<DummyCode> reader)
        {
            var timeout = reader.ReadInteger();
            return Task.Delay(timeout);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0005", diagnostics[0].Id);
        }

        [Fact]
        public async Task TimeSpan_FromSeconds_From_ReadInteger_Reports_CSL0005()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System;
    using Soulseek.Messaging;

    internal sealed class BadTimeSpan
    {
        public TimeSpan Parse(MessageReader<DummyCode> reader)
        {
            var seconds = reader.ReadInteger();
            return TimeSpan.FromSeconds(seconds);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0005", diagnostics[0].Id);
        }

        [Fact]
        public async Task TaskDelay_From_Validated_Timeout_Does_Not_Report()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.Threading.Tasks;
    using Soulseek.Messaging;

    internal sealed class GoodDelay
    {
        public Task Parse(MessageReader<DummyCode> reader)
        {
            var timeout = ProtocolValueValidator.ValidateTimeout(reader.ReadInteger());
            return Task.Delay(timeout);
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
                assemblyName: "TaintToTimeoutProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new TaintToTimeoutAnalyzer();
            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == TaintToTimeoutAnalyzer.DiagnosticId).ToImmutableArray();
        }
    }
}
