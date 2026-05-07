// <copyright file="TaintToEnumAnalyzerTests.cs" company="slskdN Team">
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

    public class TaintToEnumAnalyzerTests
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
        public static DummyStatus ValidateDefinedEnum(DummyStatus value) => value;
    }

    internal enum DummyCode { None }
    internal enum DummyStatus { None, Good }
    internal static class MessageCode
    {
        internal enum Distributed { None, SearchRequest }
    }
}
";

        [Fact]
        public async Task EnumCast_From_ReadInteger_Reports_CSL0007()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class BadCast
    {
        public DummyStatus Parse(MessageReader<DummyCode> reader)
        {
            var status = reader.ReadInteger();
            return (DummyStatus)status;
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0007", diagnostics[0].Id);
        }

        [Fact]
        public async Task EnumParse_From_ReadString_Reports_CSL0007()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System;
    using Soulseek.Messaging;

    internal sealed class BadParse
    {
        public object Parse(MessageReader<DummyCode> reader)
        {
            var status = reader.ReadString();
            return Enum.Parse(typeof(DummyStatus), status);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0007", diagnostics[0].Id);
        }

        [Fact]
        public async Task EnumCast_From_Validated_Value_Does_Not_Report()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class GoodCast
    {
        public DummyStatus Parse(MessageReader<DummyCode> reader)
        {
            return ProtocolValueValidator.ValidateDefinedEnum((DummyStatus)reader.ReadInteger());
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task MessageCode_Cast_From_ReadInteger_Does_Not_Report()
        {
            var source = Harness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using Soulseek.Messaging;

    internal sealed class DispatchCode
    {
        public MessageCode.Distributed Parse(MessageReader<DummyCode> reader)
        {
            return (MessageCode.Distributed)reader.ReadInteger();
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
                assemblyName: "TaintToEnumProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new TaintToEnumAnalyzer();
            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == TaintToEnumAnalyzer.DiagnosticId).ToImmutableArray();
        }
    }
}
