// <copyright file="BuildTask.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.CodeQuality
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     MSBuild task for running static analysis during build.
    /// </summary>
    /// <remarks>
    ///     H-CODE02: Introduce Static Analysis and Linting.
    ///     Integrates static analysis into the build pipeline.
    /// </remarks>
    public class CodeAnalysisBuildTask : Task
    {
        /// <summary>
        ///     Gets or sets the project directory.
        /// </summary>
        [Required]
        public string? ProjectDirectory { get; set; }

        /// <summary>
        ///     Gets or sets the output assembly path.
        /// </summary>
        [Required]
        public string? AssemblyPath { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to treat warnings as errors.
        /// </summary>
        public bool TreatWarningsAsErrors { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of violations allowed.
        /// </summary>
        public int MaxViolations { get; set; } = 100;

        /// <summary>
        ///     Gets or sets the paths to exclude from analysis.
        /// </summary>
        public string[]? ExcludedPaths { get; set; }

        /// <summary>
        ///     Gets or sets the analysis rules to exclude.
        /// </summary>
        public string[]? ExcludedRules { get; set; }

        /// <summary>
        ///     Gets or sets the source files passed by the project compiler.
        /// </summary>
        public string[]? SourceFiles { get; set; }

        /// <summary>
        ///     Gets or sets the compiler reference paths used for symbol binding.
        /// </summary>
        public string[]? ReferencePaths { get; set; }

        /// <summary>
        ///     Gets or sets the C# language version used by the project.
        /// </summary>
        public string? CSharpLanguageVersion { get; set; }

        /// <summary>
        ///     Gets or sets the project preprocessor symbols.
        /// </summary>
        public string? PreprocessorSymbols { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the project allows unsafe code.
        /// </summary>
        public bool AllowUnsafe { get; set; }

        /// <summary>
        ///     Executes the build task.
        /// </summary>
        /// <returns>True if the task succeeded.</returns>
        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Normal, "Running slskdN Code Analysis...");

                var defaultConfig = AnalyzerConfiguration.Default;
                var config = new AnalyzerConfig
                {
                    Rules = defaultConfig.Rules,
                    TreatWarningsAsErrors = TreatWarningsAsErrors,
                    MaxViolationsPerFile = defaultConfig.MaxViolationsPerFile,
                    ExcludedPaths = (ExcludedPaths != null && ExcludedPaths.Length > 0) ? ExcludedPaths : defaultConfig.ExcludedPaths,
                    ExcludedRules = (ExcludedRules != null && ExcludedRules.Length > 0) ? ExcludedRules : defaultConfig.ExcludedRules
                };

                var result = RunAnalysis(config);

                // Report results
                ReportResults(result, config);

                // Check if build should fail
                var shouldFail = ShouldFailBuild(result, config);
                if (shouldFail)
                {
                    Log.LogError("Code analysis failed. Fix violations or adjust configuration.");
                    return false;
                }

                Log.LogMessage(MessageImportance.Normal, "Code analysis completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: false, showDetail: true, file: null);
                return false;
            }
        }

        private StaticAnalysisResult RunAnalysis(AnalyzerConfig config)
        {
            var violations = new List<AnalysisViolation>();
            var totalTypesAnalyzed = 0;
            var analysisComplete = true;
            var incompleteReasons = new List<string>();

            // Analyze the built assembly
            if (!string.IsNullOrEmpty(AssemblyPath) && File.Exists(AssemblyPath))
            {
                ApplicationAssemblyLoadContext? loadContext = null;
                try
                {
                    loadContext = new ApplicationAssemblyLoadContext(AssemblyPath);
                    var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(AssemblyPath));
                    var assemblyResult = StaticAnalysis.AnalyzeAssembly(assembly);
                    totalTypesAnalyzed = assemblyResult.TotalTypesAnalyzed;
                    violations.AddRange(ApplyConfiguredRules(assemblyResult.Violations, config));
                }
                catch (Exception ex)
                {
                    analysisComplete = false;
                    var reason = GetExceptionSummary(ex);
                    incompleteReasons.Add(reason);
                    Log.LogWarning($"Failed to analyze assembly {Path.GetFileName(AssemblyPath)}: {reason}");
                }
                finally
                {
                    loadContext?.Unload();
                }
            }
            else
            {
                analysisComplete = false;
                incompleteReasons.Add("the built application assembly was not found");
            }

            // Analyze source files
            if (!string.IsNullOrEmpty(ProjectDirectory) && Directory.Exists(ProjectDirectory))
            {
                var sourceCompilation = CreateSourceCompilation(ProjectDirectory);
                violations.AddRange(AnalyzeSourceFiles(ProjectDirectory, config, sourceCompilation));
            }
            else
            {
                analysisComplete = false;
                incompleteReasons.Add("the project source directory was not found");
            }

            return new StaticAnalysisResult
            {
                AssemblyName = Path.GetFileNameWithoutExtension(AssemblyPath),
                TotalTypesAnalyzed = totalTypesAnalyzed,
                AnalysisComplete = analysisComplete,
                IncompleteReason = incompleteReasons.Count == 0 ? null : string.Join("; ", incompleteReasons),
                Violations = violations,
                AnalysisTimestamp = DateTimeOffset.UtcNow
            };
        }

        private IEnumerable<AnalysisViolation> AnalyzeSourceFiles(
            string projectDirectory,
            AnalyzerConfig config,
            CSharpCompilation? sourceCompilation)
        {
            var violations = new List<AnalysisViolation>();
            var csFiles = GetAnalyzedSourceFiles(projectDirectory);

            foreach (var csFile in csFiles)
            {
                // Check if file should be excluded
                if (IsExcludedPath(csFile, config.ExcludedPaths))
                {
                    continue;
                }

                try
                {
                    var syntaxTree = sourceCompilation?.SyntaxTrees.FirstOrDefault(tree =>
                        string.Equals(
                            Path.GetFullPath(tree.FilePath),
                            Path.GetFullPath(csFile),
                            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
                    var sourceCode = syntaxTree is null ? File.ReadAllText(csFile) : null;
                    var sourceViolations = syntaxTree is null
                        ? BuildTimeAnalyzer.AnalyzeSourceCode(sourceCode!, csFile)
                        : BuildTimeAnalyzer.AnalyzeSyntaxTree(
                            syntaxTree,
                            csFile,
                            sourceCompilation!.GetSemanticModel(syntaxTree, ignoreAccessibility: true));
                    var fileViolations = sourceViolations
                        .Where(violation => IsRuleEnabled(violation.Rule, config))
                        .Take(config.MaxViolationsPerFile);

                    foreach (var violation in fileViolations)
                    {
                        if (TryGetRuleConfiguration(violation.Rule, config, out var rule))
                        {
                            violations.Add(new AnalysisViolation
                            {
                                Location = $"{GetRelativePath(csFile, projectDirectory)}:{violation.LineNumber}",
                                Rule = violation.Rule,
                                Severity = rule.Severity,
                                Message = violation.Message,
                                Recommendation = violation.Recommendation
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Failed to analyze file {csFile}: {ex.Message}");
                }
            }

            return violations;
        }

        private CSharpCompilation? CreateSourceCompilation(string projectDirectory)
        {
            try
            {
                var parseOptions = CreateParseOptions();
                var syntaxTrees = GetCompilationSourceFiles(projectDirectory)
                    .Where(File.Exists)
                    .Distinct(StringComparer.Ordinal)
                    .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path))
                    .ToArray();
                if (syntaxTrees.Length == 0)
                {
                    return null;
                }

                var metadataReferences = GetMetadataReferences();
                if (metadataReferences.Count == 0)
                {
                    Log.LogWarning("Semantic source analysis has no compiler references; using syntax-only findings.");
                    return null;
                }

                return CSharpCompilation.Create(
                    assemblyName: "slskd.CodeQuality.SourceAnalysis",
                    syntaxTrees: syntaxTrees,
                    references: metadataReferences,
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        allowUnsafe: AllowUnsafe,
                        nullableContextOptions: NullableContextOptions.Enable));
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Semantic source analysis could not create a Roslyn compilation: {ex.Message}");
                return null;
            }
        }

        private CSharpParseOptions CreateParseOptions()
        {
            var languageVersion = LanguageVersion.Default;
            if (!string.IsNullOrWhiteSpace(CSharpLanguageVersion) &&
                LanguageVersionFacts.TryParse(CSharpLanguageVersion, out var parsedLanguageVersion))
            {
                languageVersion = parsedLanguageVersion;
            }

            var preprocessorSymbols = string.IsNullOrWhiteSpace(PreprocessorSymbols)
                ? Array.Empty<string>()
                : PreprocessorSymbols.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new CSharpParseOptions(languageVersion, preprocessorSymbols: preprocessorSymbols);
        }

        private List<MetadataReference> GetMetadataReferences()
        {
            var referencePaths = (ReferencePaths ?? Array.Empty<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (ReferencePaths is not { Length: > 0 } && AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies)
            {
                referencePaths.AddRange(trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));
            }

            if (!string.IsNullOrWhiteSpace(AssemblyPath) && File.Exists(AssemblyPath))
            {
                referencePaths.Add(AssemblyPath);
            }

            var references = new List<MetadataReference>(referencePaths.Count);
            foreach (var path in referencePaths)
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(Path.GetFullPath(path)));
                }
                catch (Exception ex)
                {
                    Log.LogMessage(MessageImportance.Low, $"Ignoring unavailable compiler reference {Path.GetFileName(path)}: {ex.Message}");
                }
            }

            return references;
        }

        private IEnumerable<string> GetCompilationSourceFiles(string projectDirectory)
        {
            return SourceFiles is { Length: > 0 }
                ? SourceFiles.Select(path => Path.IsPathRooted(path) ? path : Path.Combine(projectDirectory, path))
                : Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
        }

        private IEnumerable<string> GetAnalyzedSourceFiles(string projectDirectory)
        {
            return GetCompilationSourceFiles(projectDirectory)
                .Where(path => string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                .Where(File.Exists)
                .Distinct(StringComparer.Ordinal);
        }

        private bool IsExcludedPath(string filePath, IEnumerable<string> excludedPaths)
        {
            var relativePath = GetRelativePath(filePath, ProjectDirectory ?? string.Empty);
            return excludedPaths.Any(excluded =>
                relativePath.Contains(excluded, StringComparison.OrdinalIgnoreCase));
        }

        private string GetRelativePath(string fullPath, string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                return fullPath;
            }

            var baseUri = new Uri(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar);
            var fullUri = new Uri(Path.GetFullPath(fullPath));

            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString());
        }

        private void ReportResults(StaticAnalysisResult result, AnalyzerConfig config)
        {
            var violationsBySeverity = result.ViolationsBySeverity;

            Log.LogMessage(MessageImportance.Normal, "Analysis Results:");
            Log.LogMessage(MessageImportance.Normal, $"  Types Analyzed: {result.TotalTypesAnalyzed}");
            Log.LogMessage(MessageImportance.Normal, $"  Analysis Complete: {result.AnalysisComplete}");
            Log.LogMessage(MessageImportance.Normal, $"  Total Violations: {result.Violations.Count}");

            foreach (var kvp in violationsBySeverity.OrderByDescending(v => v.Key))
            {
                var severity = kvp.Key;
                var count = kvp.Value;
                Log.LogMessage(MessageImportance.Normal, $"  {severity}: {count}");
            }

            // Log individual violations
            foreach (var violation in result.Violations.OrderByDescending(v => v.Severity))
            {
                var importance = violation.Severity switch
                {
                    ViolationSeverity.Error => MessageImportance.High,
                    ViolationSeverity.Warning => MessageImportance.Normal,
                    ViolationSeverity.Info => MessageImportance.Low,
                    _ => MessageImportance.Low
                };

                Log.LogMessage(importance, $"{violation.Severity}: {violation.Rule} - {violation.Message}");
                Log.LogMessage(importance, $"  Location: {violation.Location}");

                if (!string.IsNullOrEmpty(violation.Recommendation))
                {
                    Log.LogMessage(importance, $"  Recommendation: {violation.Recommendation}");
                }
            }
        }

        private bool ShouldFailBuild(StaticAnalysisResult result, AnalyzerConfig config)
        {
            if (!result.AnalysisComplete)
            {
                Log.LogError($"Static analysis was incomplete: {result.IncompleteReason}");
                return true;
            }

            var errorCount = result.Violations.Count(v => v.Severity == ViolationSeverity.Error);
            var warningCount = result.Violations.Count(v => v.Severity == ViolationSeverity.Warning);
            var gatedViolationCount = errorCount + (config.TreatWarningsAsErrors ? warningCount : 0);

            // Check total violations
            if (gatedViolationCount > MaxViolations)
            {
                Log.LogError($"Too many build-blocking violations: {gatedViolationCount} > {MaxViolations}");
                return true;
            }

            // Check for errors
            if (errorCount > 0)
            {
                Log.LogError($"Found {errorCount} error-level violations");
                return true;
            }

            // Check warnings as errors
            if (config.TreatWarningsAsErrors)
            {
                if (warningCount > 0)
                {
                    Log.LogError($"Found {warningCount} warning-level violations (treated as errors)");
                    return true;
                }
            }

            return false;
        }

        private static bool IsRuleEnabled(string? ruleName, AnalyzerConfig config)
        {
            return TryGetRuleConfiguration(ruleName, config, out _);
        }

        private static IEnumerable<AnalysisViolation> ApplyConfiguredRules(
            IEnumerable<AnalysisViolation> violations,
            AnalyzerConfig config)
        {
            foreach (var violation in violations)
            {
                if (TryGetRuleConfiguration(violation.Rule, config, out var rule))
                {
                    yield return new AnalysisViolation
                    {
                        Location = violation.Location,
                        Rule = violation.Rule,
                        Severity = rule.Severity,
                        Message = violation.Message,
                        Recommendation = violation.Recommendation
                    };
                }
            }
        }

        private static bool TryGetRuleConfiguration(string? ruleName, AnalyzerConfig config, out RuleConfig rule)
        {
            if (!string.IsNullOrWhiteSpace(ruleName) &&
                !config.ExcludedRules.Contains(ruleName, StringComparer.OrdinalIgnoreCase) &&
                config.Rules.TryGetValue(ruleName, out var found) &&
                found.IsEnabled)
            {
                rule = found;
                return true;
            }

            rule = null!;
            return false;
        }

        private static string GetExceptionSummary(Exception exception)
        {
            if (exception is ReflectionTypeLoadException typeLoadException)
            {
                var loaderErrors = typeLoadException.LoaderExceptions
                    .OfType<Exception>()
                    .Select(loaderException => loaderException.Message)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct(StringComparer.Ordinal)
                    .Take(5)
                    .ToList();

                if (loaderErrors.Count > 0)
                {
                    return string.Join("; ", loaderErrors);
                }
            }

            return exception.Message;
        }

        private sealed class ApplicationAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _dependencyResolver;
            private readonly string _aspNetCoreFrameworkDirectory;

            public ApplicationAssemblyLoadContext(string applicationAssemblyPath)
                : base("slskd-static-analysis", isCollectible: true)
            {
                _dependencyResolver = new AssemblyDependencyResolver(Path.GetFullPath(applicationAssemblyPath));
                _aspNetCoreFrameworkDirectory = GetAspNetCoreFrameworkDirectory();
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                var resolvedPath = _dependencyResolver.ResolveAssemblyToPath(assemblyName);
                if (resolvedPath != null)
                {
                    return LoadFromAssemblyPath(resolvedPath);
                }

                if (!string.IsNullOrWhiteSpace(assemblyName.Name))
                {
                    var sharedFrameworkPath = Path.Combine(_aspNetCoreFrameworkDirectory, $"{assemblyName.Name}.dll");
                    if (File.Exists(sharedFrameworkPath))
                    {
                        return LoadFromAssemblyPath(sharedFrameworkPath);
                    }
                }

                return null;
            }

            private static string GetAspNetCoreFrameworkDirectory()
            {
                var runtimeDirectory = Path.TrimEndingDirectorySeparator(RuntimeEnvironment.GetRuntimeDirectory());
                var runtimeFrameworkDirectory = Directory.GetParent(runtimeDirectory);
                var sharedFrameworkRoot = runtimeFrameworkDirectory?.Parent;
                if (sharedFrameworkRoot == null)
                {
                    return string.Empty;
                }

                return Path.Combine(
                    sharedFrameworkRoot.FullName,
                    "Microsoft.AspNetCore.App",
                    Path.GetFileName(runtimeDirectory));
            }
        }
    }
}
