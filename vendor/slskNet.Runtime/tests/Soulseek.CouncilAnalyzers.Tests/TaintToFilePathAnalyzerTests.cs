// <copyright file="TaintToFilePathAnalyzerTests.cs" company="slskdN Team">
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

    public class TaintToFilePathAnalyzerTests
    {
        private const string ReaderHarness = @"
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

    internal static class PathSafety
    {
        public static string ResolveContainedPath(string root, string relativePath) => relativePath;
    }

    internal enum DummyCode { None }
}
";

        [Fact]
        public async Task File_ReadAllText_From_ReadString_Reports_CSL0004()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.IO;
    using System.Linq;
    using Soulseek.Messaging;

    internal sealed class BadFileRead
    {
        public string Parse(MessageReader<DummyCode> reader)
        {
            var name = reader.ReadString();
            return File.ReadAllText(name);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0004", diagnostics[0].Id);
        }

        [Fact]
        public async Task Directory_EnumerateFiles_From_Combined_ReadString_Reports_CSL0004()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.IO;
    using Soulseek.Messaging;

    internal sealed class BadDirectoryRead
    {
        public string[] Parse(MessageReader<DummyCode> reader, string root)
        {
            var name = reader.ReadString();
            var path = Path.Combine(root, name);
            return Directory.EnumerateFiles(path).ToArray();
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0004", diagnostics[0].Id);
        }

        [Fact]
        public async Task FileStream_From_ReadString_Reports_CSL0004()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.IO;
    using Soulseek.Messaging;

    internal sealed class BadFileStream
    {
        public FileStream Parse(MessageReader<DummyCode> reader)
        {
            var name = reader.ReadString();
            return new FileStream(name, FileMode.Open);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Single(diagnostics);
            Assert.Equal("CSL0004", diagnostics[0].Id);
        }

        [Fact]
        public async Task File_ReadAllText_From_Contained_Path_Does_Not_Report()
        {
            var source = ReaderHarness + @"
namespace Soulseek.Messaging.Messages.Server
{
    using System.IO;
    using Soulseek.Messaging;

    internal sealed class GoodFileRead
    {
        public string Parse(MessageReader<DummyCode> reader, string root)
        {
            var path = PathSafety.ResolveContainedPath(root, reader.ReadString());
            return File.ReadAllText(path);
        }
    }
}
";
            var diagnostics = await RunAnalyzerAsync(source);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task File_ReadAllText_From_Parameter_Does_Not_Report()
        {
            var source = @"
namespace Probe
{
    using System.IO;

    internal sealed class ParamFile
    {
        public string Read(string path) => File.ReadAllText(path);
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
                assemblyName: "TaintToFilePathProbe",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new TaintToFilePathAnalyzer();
            var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return diagnostics.Where(d => d.Id == TaintToFilePathAnalyzer.DiagnosticId).ToImmutableArray();
        }
    }
}
