// <copyright file="AnalyzerConfiguration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.CodeQuality
{
    using System.Collections.Generic;

    /// <summary>
    ///     Configuration for static analysis rules and severity levels.
    /// </summary>
    /// <remarks>
    ///     H-CODE02: Introduce Static Analysis and Linting.
    ///     Defines analysis rules aligned with docs/engineering-standards.md.
    /// </remarks>
    public static class AnalyzerConfiguration
    {
        /// <summary>
        ///     Gets the default analyzer configuration.
        /// </summary>
        public static AnalyzerConfig Default { get; } = CreateDefaultConfiguration();

        /// <summary>
        ///     Creates the default analyzer configuration.
        /// </summary>
        /// <returns>The default configuration.</returns>
        private static AnalyzerConfig CreateDefaultConfiguration()
        {
            var rules = new Dictionary<string, RuleConfig>
            {
                // Async/IO Rules (from H-CODE01)
                ["BlockingAsyncCall"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Async",
                    Description = "Syntactically identifiable blocking Task/ValueTask operations",
                    IsEnabled = true
                },

                // Security Rules
                ["DangerousApiUsage"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Security",
                    Description = "Usage of potentially dangerous APIs (ExecuteSqlRaw, Process.Start, etc.)",
                    IsEnabled = true
                },

                ["PotentialSqlInjection"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Security",
                    Description = "SQL interpolation heuristic pending semantic execution-sink analysis",
                    IsEnabled = false
                },

                ["ExposesSensitiveData"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Security",
                    Description = "Property name/type heuristic pending explicit sensitivity or data-flow analysis",
                    IsEnabled = false
                },

                ["MissingParameterValidation"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Security",
                    Description = "String parameter names that may indicate an input boundary",
                    IsEnabled = false
                },

                // Code Quality Rules
                ["AsyncNaming"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Naming",
                    Description = "Task-returning methods whose names end with 'Sync'",
                    IsEnabled = true
                },

                ["MissingCancellationToken"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Async",
                    Description = "Task-returning methods missing CancellationToken parameters",
                    IsEnabled = false
                },

                ["MutablePublicProperty"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Info,
                    Category = "Design",
                    Description = "Public mutable properties (encapsulation violation)",
                    IsEnabled = false
                },

                ["LargeClass"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Info,
                    Category = "Design",
                    Description = "Classes with too many public methods",
                    IsEnabled = false
                },

                ["TooManyParameters"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Info,
                    Category = "Design",
                    Description = "Methods with too many parameters",
                    IsEnabled = false
                },

                // Performance Rules
                ["ExpensiveOperation"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Info,
                    Category = "Performance",
                    Description = "Potentially expensive operations in hot paths",
                    IsEnabled = false
                },

                ["InefficientStringConcatenation"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Performance",
                    Description = "String concatenation syntax inside loops, without type information",
                    IsEnabled = false
                },

                // Exception Handling Rules
                ["EmptyCatchBlock"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Warning,
                    Category = "Exception",
                    Description = "Unfiltered empty catch blocks that swallow exceptions",
                    IsEnabled = true
                },

                ["LoggingOnlyCatch"] = new RuleConfig
                {
                    Severity = ViolationSeverity.Info,
                    Category = "Exception",
                    Description = "Catch blocks that only log and rethrow",
                    IsEnabled = false
                },

            };

            return new AnalyzerConfig
            {
                Rules = rules,
                TreatWarningsAsErrors = false,
                MaxViolationsPerFile = 50,
                ExcludedPaths = new[] { "obj/", "bin/", "node_modules/", ".git/" },
                ExcludedRules = new string[0]
            };
        }

        /// <summary>
        ///     Gets a rule configuration by name.
        /// </summary>
        /// <param name="ruleName">The name of the rule.</param>
        /// <returns>The rule configuration, or null if not found.</returns>
        public static RuleConfig? GetRuleConfig(string ruleName)
        {
            return Default.Rules.TryGetValue(ruleName, out var config) ? config : null;
        }

        /// <summary>
        ///     Checks if a rule is enabled.
        /// </summary>
        /// <param name="ruleName">The name of the rule.</param>
        /// <returns>True if the rule is enabled.</returns>
        public static bool IsRuleEnabled(string ruleName)
        {
            return GetRuleConfig(ruleName)?.IsEnabled ?? false;
        }

        /// <summary>
        ///     Gets the severity for a rule.
        /// </summary>
        /// <param name="ruleName">The name of the rule.</param>
        /// <returns>The severity level, or Info if rule not found.</returns>
        public static ViolationSeverity GetRuleSeverity(string ruleName)
        {
            return GetRuleConfig(ruleName)?.Severity ?? ViolationSeverity.Info;
        }
    }

    /// <summary>
    ///     Configuration for the static analyzer.
    /// </summary>
    public sealed class AnalyzerConfig
    {
        /// <summary>
        ///     Gets the rule configurations.
        /// </summary>
        public required IReadOnlyDictionary<string, RuleConfig> Rules { get; init; }

        /// <summary>
        ///     Gets a value indicating whether to treat warnings as errors.
        /// </summary>
        public bool TreatWarningsAsErrors { get; init; }

        /// <summary>
        ///     Gets the maximum number of violations allowed per file.
        /// </summary>
        public int MaxViolationsPerFile { get; init; }

        /// <summary>
        ///     Gets the paths to exclude from analysis.
        /// </summary>
        public IReadOnlyList<string> ExcludedPaths { get; init; } = Array.Empty<string>();

        /// <summary>
        ///     Gets the rules to exclude from analysis.
        /// </summary>
        public IReadOnlyList<string> ExcludedRules { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    ///     Configuration for a specific analysis rule.
    /// </summary>
    public sealed class RuleConfig
    {
        /// <summary>
        ///     Gets the severity level for violations of this rule.
        /// </summary>
        public ViolationSeverity Severity { get; init; }

        /// <summary>
        ///     Gets the category this rule belongs to.
        /// </summary>
        public string? Category { get; init; }

        /// <summary>
        ///     Gets the description of what this rule checks.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        ///     Gets a value indicating whether this rule is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }
    }
}
