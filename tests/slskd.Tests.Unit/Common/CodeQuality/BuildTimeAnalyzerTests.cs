// <copyright file="BuildTimeAnalyzerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
extern alias BuildTasks;

namespace slskd.Tests.Unit.Common.CodeQuality
{
    using Microsoft.Build.Framework;
    using Moq;
    using System.Linq;
    using slskd.Common.CodeQuality;
    using CodeAnalysisBuildTask = BuildTasks::slskd.Common.CodeQuality.CodeAnalysisBuildTask;
    using BuildTimeAnalyzer = BuildTasks::slskd.Common.CodeQuality.BuildTimeAnalyzer;
    using BuildToolsViolationSeverity = BuildTasks::slskd.Common.CodeQuality.ViolationSeverity;
    using Xunit;

    /// <summary>
    ///     Tests for H-CODE02: BuildTimeAnalyzer implementation.
    /// </summary>
    public class BuildTimeAnalyzerTests
    {
        [Fact]
        public void BuildTimeAnalyzer_IsOwnedByBuildTools_AndRuntimeHasNoRoslynReferences()
        {
            Assert.Equal("slskd.BuildTasks", typeof(BuildTimeAnalyzer).Assembly.GetName().Name);

            var runtimeRoslynReferences = typeof(SafeControllerFeatureProvider).Assembly
                .GetReferencedAssemblies()
                .Where(reference => reference.Name?.StartsWith("Microsoft.CodeAnalysis", System.StringComparison.Ordinal) == true)
                .ToList();

            Assert.Empty(runtimeRoslynReferences);
        }

        [Fact]
        public void CodeAnalysisBuildTask_ReportsAssemblyLoadFailureWithoutThrowingDuringLogging()
        {
            var assemblyPath = System.IO.Path.GetTempFileName();
            var projectDirectory = System.IO.Directory.CreateTempSubdirectory();
            var buildEngine = new Mock<IBuildEngine>();
            BuildWarningEventArgs? capturedWarning = null;
            var capturedErrors = new System.Collections.Generic.List<BuildErrorEventArgs>();
            buildEngine
                .Setup(engine => engine.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()))
                .Callback<BuildWarningEventArgs>(warning => capturedWarning = warning);
            buildEngine
                .Setup(engine => engine.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(capturedErrors.Add);

            try
            {
                var task = new CodeAnalysisBuildTask
                {
                    ProjectDirectory = projectDirectory.FullName,
                    AssemblyPath = assemblyPath,
                };
                task.BuildEngine = buildEngine.Object;

                Assert.False(task.Execute());
                Assert.NotNull(capturedWarning);
                Assert.Contains("Failed to analyze assembly", capturedWarning.Message);
                Assert.Contains(capturedErrors, error => error.Message.Contains("Static analysis was incomplete"));
            }
            finally
            {
                System.IO.File.Delete(assemblyPath);
                System.IO.Directory.Delete(projectDirectory.FullName, recursive: true);
            }
        }

        [Fact]
        public void CodeAnalysisBuildTask_LoadsApplicationFrameworkDependenciesForReflection()
        {
            var projectDirectory = System.IO.Directory.CreateTempSubdirectory();
            var buildEngine = new Mock<IBuildEngine>();
            var warnings = new System.Collections.Generic.List<BuildWarningEventArgs>();
            var messages = new System.Collections.Generic.List<BuildMessageEventArgs>();
            buildEngine
                .Setup(engine => engine.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()))
                .Callback<BuildWarningEventArgs>(warnings.Add);
            buildEngine
                .Setup(engine => engine.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback<BuildMessageEventArgs>(messages.Add);

            try
            {
                var task = new CodeAnalysisBuildTask
                {
                    ProjectDirectory = projectDirectory.FullName,
                    AssemblyPath = typeof(SafeControllerFeatureProvider).Assembly.Location,
                    MaxViolations = int.MaxValue,
                };
                task.BuildEngine = buildEngine.Object;

                Assert.True(task.Execute());
                Assert.DoesNotContain(warnings, warning => warning.Message.Contains("Failed to analyze assembly"));
                Assert.Contains(messages, message =>
                {
                    var marker = "Types Analyzed: ";
                    var index = message.Message.IndexOf(marker, System.StringComparison.Ordinal);
                    return index >= 0 &&
                        int.TryParse(message.Message[(index + marker.Length)..], out var count) &&
                        count > 0;
                });
            }
            finally
            {
                projectDirectory.Delete(recursive: true);
            }
        }

        [Fact]
        public void CodeAnalysisBuildTask_ReportsRelativePathsForSameNamedSourceFiles()
        {
            var projectDirectory = System.IO.Directory.CreateTempSubdirectory();
            var commonDirectory = projectDirectory.CreateSubdirectory("Common");
            var meshDirectory = projectDirectory.CreateSubdirectory("Mesh");
            const string sourceCode = "using System.Threading.Tasks; public class Target { public string Get(Task<string> task) => task.Result; }";
            System.IO.File.WriteAllText(System.IO.Path.Combine(commonDirectory.FullName, "ContentSafety.cs"), sourceCode);
            System.IO.File.WriteAllText(System.IO.Path.Combine(meshDirectory.FullName, "ContentSafety.cs"), sourceCode);

            var buildEngine = new Mock<IBuildEngine>();
            var messages = new System.Collections.Generic.List<BuildMessageEventArgs>();
            buildEngine
                .Setup(engine => engine.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback<BuildMessageEventArgs>(messages.Add);

            try
            {
                var task = new CodeAnalysisBuildTask
                {
                    ProjectDirectory = projectDirectory.FullName,
                    AssemblyPath = typeof(SafeControllerFeatureProvider).Assembly.Location,
                    MaxViolations = int.MaxValue,
                };
                task.BuildEngine = buildEngine.Object;

                Assert.True(task.Execute());
                Assert.Contains(messages, message => message.Message.Contains("Location: Common/ContentSafety.cs:", System.StringComparison.Ordinal));
                Assert.Contains(messages, message => message.Message.Contains("Location: Mesh/ContentSafety.cs:", System.StringComparison.Ordinal));
            }
            finally
            {
                projectDirectory.Delete(recursive: true);
            }
        }

        [Fact]
        public void CodeAnalysisBuildTask_UsesResolvedSymbolsForTaskAndDangerousApiFindings()
        {
            var projectDirectory = System.IO.Directory.CreateTempSubdirectory();
            var buildEngine = new Mock<IBuildEngine>();
            var messages = new System.Collections.Generic.List<BuildMessageEventArgs>();
            buildEngine
                .Setup(engine => engine.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback<BuildMessageEventArgs>(messages.Add);

            const string sourceCode = """
                namespace Demo;

                public sealed class Task
                {
                    public int Result => 1;
                    public void Wait() { }
                }

                public static class Targets
                {
                    private static System.Threading.Tasks.Task<int> LoadAsync() =>
                        System.Threading.Tasks.Task.FromResult(1);

                    public static int ReadAsyncResult() => LoadAsync().Result;
                    public static void WaitForAsync() => LoadAsync().Wait();
                    public static int ReadAsyncValue() => LoadAsync().GetAwaiter().GetResult();

                    private static Task LoadFakeTask() => new();
                    public static int ReadFakeResult() => LoadFakeTask().Result;
                    public static void WaitForFakeTask() => LoadFakeTask().Wait();

                    public static void StartProcess() => System.Diagnostics.Process.Start("dotnet");
                    public static void StartFakeProcess() => FakeProcess.Start("dotnet");
                }

                public static class FakeProcess
                {
                    public static void Start(string fileName) { }
                }
                """;
            System.IO.File.WriteAllText(System.IO.Path.Combine(projectDirectory.FullName, "SemanticTargets.cs"), sourceCode);

            try
            {
                var task = new CodeAnalysisBuildTask
                {
                    ProjectDirectory = projectDirectory.FullName,
                    AssemblyPath = typeof(SafeControllerFeatureProvider).Assembly.Location,
                    ReferencePaths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?
                        .Split(System.IO.Path.PathSeparator, System.StringSplitOptions.RemoveEmptyEntries),
                    MaxViolations = int.MaxValue,
                };
                task.BuildEngine = buildEngine.Object;

                Assert.True(task.Execute());
                var sourceLocations = messages
                    .Select(message => message.Message)
                    .Where(message => message.Contains("Location: SemanticTargets.cs:", System.StringComparison.Ordinal))
                    .ToList();
                Assert.Equal(4, sourceLocations.Count);
            }
            finally
            {
                projectDirectory.Delete(recursive: true);
            }
        }

        [Fact]
        public void AnalyzeSourceCode_WithBlockingAsyncCall_ReturnsViolation()
        {
            // Arrange
            const string sourceCode = @"
public class TestClass
{
    public async System.Threading.Tasks.Task MyMethod()
    {
        var result = SomeAsyncMethod().Result; // Blocking call
    }

    private System.Threading.Tasks.Task<string> SomeAsyncMethod()
    {
        return System.Threading.Tasks.Task.FromResult(""test"");
    }
}";

            // Act
            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            // Assert
            Assert.Contains(violations, v => v.Rule == "BlockingAsyncCall");
            Assert.Contains(violations, v => v.Message.Contains("Blocking async call detected"));
        }

        [Fact]
        public void AnalyzeSourceCode_WithWaitCall_ReturnsViolation()
        {
            // Arrange
            const string sourceCode = @"
public class TestClass
{
    public void MyMethod()
    {
        SomeAsyncMethod().Wait(); // Blocking call
    }

    private System.Threading.Tasks.Task SomeAsyncMethod()
    {
        return System.Threading.Tasks.Task.CompletedTask;
    }
}";

            // Act
            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            // Assert
            Assert.Contains(violations, v => v.Rule == "BlockingAsyncCall");
            Assert.Contains(violations, v => v.Message.Contains("Blocking async call detected"));
        }

        [Fact]
        public void AnalyzeSourceCode_WithTaskGetAwaiterGetResult_ReturnsViolation()
        {
            const string sourceCode = @"
public class TestClass
{
    public string MyMethod() => SomeAsyncMethod().GetAwaiter().GetResult();

    private System.Threading.Tasks.Task<string> SomeAsyncMethod()
    {
        return System.Threading.Tasks.Task.FromResult(""test"");
    }
}";

            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            Assert.Contains(violations, violation => violation.Rule == "BlockingAsyncCall");
        }

        [Fact]
        public void AnalyzeSourceCode_WithNonTaskResultAndZeroTimeoutWait_DoesNotReturnBlockingViolation()
        {
            const string sourceCode = @"
using System.Threading;
using System.Threading.Tasks;
public class TestClass
{
    private readonly Task pending = Task.CompletedTask;
    private readonly SemaphoreSlim limiter = new SemaphoreSlim(1, 1);

    public bool TryWait()
    {
        var context = new ActionContext();
        context.Result = new object();
        return pending.Wait(0) && limiter.Wait(0);
    }
}
public class ActionContext
{
    public object Result { get; set; }
}";

            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            Assert.DoesNotContain(violations, violation => violation.Rule == "BlockingAsyncCall");
        }

        [Fact]
        public void AnalyzeSourceCode_WithGenericTaskWaiter_DoesNotReturnBlockingViolation()
        {
            const string sourceCode = @"
public class TestClass
{
    public async System.Threading.Tasks.Task<string> LoadAsync()
    {
        return await Waiter.Wait<string>();
    }
}";

            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            Assert.DoesNotContain(violations, violation => violation.Rule == "BlockingAsyncCall");
        }

        [Fact]
        public void AnalyzeSourceCode_WithSqlInjection_ReturnsViolation()
        {
            // Arrange
            const string sourceCode = @"
public class TestClass
{
    public void MyMethod(string userInput)
    {
        var query = $""SELECT * FROM Users WHERE Name = '{userInput}'""; // SQL injection
    }
}";

            // Act
            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            // Assert
            Assert.Contains(violations, v => v.Rule == "PotentialSqlInjection");
        }

        [Fact]
        public void AnalyzeSourceCode_WithEmptyCatchBlock_ReturnsViolation()
        {
            // Arrange
            const string sourceCode = @"
public class TestClass
{
    public void MyMethod()
    {
        try
        {
            DoSomething();
        }
        catch
        {
            // Empty catch block
        }
    }

    private void DoSomething() { }
}";

            // Act
            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            // Assert
            Assert.Contains(violations, v => v.Rule == "EmptyCatchBlock");
        }

        [Fact]
        public void AnalyzeSourceCode_WithFilteredEmptyCatch_DoesNotReturnViolation()
        {
            const string sourceCode = @"
public class TestClass
{
    public void MyMethod()
    {
        try { }
        catch (System.OperationCanceledException) when (true) { }
    }
}";

            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            Assert.DoesNotContain(violations, violation => violation.Rule == "EmptyCatchBlock");
        }

        [Fact]
        public void AnalyzeSourceCode_WithStringConcatInLoop_ReturnsViolation()
        {
            // Arrange
            const string sourceCode = @"
public class TestClass
{
    public void MyMethod()
    {
        var result = string.Empty;
        for (int i = 0; i < 10; i++)
        {
            result = result + i.ToString(); // Inefficient string concat in loop
        }
    }
}";

            // Act
            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            // Assert
            Assert.Contains(violations, v => v.Rule == "InefficientStringConcatenation");
        }

        [Fact]
        public void AnalyzeSourceCode_DoesNotDemandDefensiveNullChecksForInternalParameters()
        {
            // Arrange
            const string sourceCode = @"
public class TestClass
{
    public void MyMethod(string input)
    {
        // No null check for input parameter
        var length = input.Length;
    }
}";

            // Act
            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            // Assert
            Assert.DoesNotContain(violations, v => v.Rule == "MissingNullCheck");
        }

        [Fact]
        public void AnalyzeSourceCode_WithValidCode_ReturnsNoViolations()
        {
            // Arrange
            const string sourceCode = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task MyMethod(string input)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException(nameof(input));
        }

        await Task.Delay(100);
    }
}";

            // Act
            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            // Assert
            // Should not have critical violations
            Assert.DoesNotContain(violations, v => v.Severity == BuildToolsViolationSeverity.Error);
        }

        [Fact]
        public void AnalyzeSourceCode_WithDangerousApiUsage_ReturnsViolation()
        {
            // Arrange
            const string sourceCode = @"
public class TestClass
{
    public void MyMethod()
    {
        // Simulate dangerous API usage (member access so analyzer detects DangerousApiUsage)
        this.ExecuteSqlRaw(""SELECT * FROM Users"");
    }

    private void ExecuteSqlRaw(string sql) { }
}";

            // Act
            var violations = BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode, "TestFile.cs").ToList();

            // Assert
            Assert.Contains(violations, v => v.Rule == "DangerousApiUsage");
        }
    }
}
