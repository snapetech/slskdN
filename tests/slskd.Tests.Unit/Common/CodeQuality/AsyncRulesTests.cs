// <copyright file="AsyncRulesTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.CodeQuality
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using slskd.Common.CodeQuality;
    using Xunit;

    /// <summary>
    ///     Tests for H-CODE01: AsyncRules implementation.
    /// </summary>
    public class AsyncRulesTests
    {
        [Fact]
        public async Task ValidateCancellationHandlingAsync_WithProperCancellation_ReturnsTrue()
        {
            Task TestOperationAsync(CancellationToken ct)
            {
                var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                ct.Register(static state =>
                {
                    var tuple = ((TaskCompletionSource<object?> Completion, CancellationToken Token))state!;
                    tuple.Completion.TrySetCanceled(tuple.Token);
                }, (completion, ct));
                return completion.Task;
            }

            var result = await AsyncRules.ValidateCancellationHandlingAsync(TestOperationAsync, TimeSpan.FromMilliseconds(100));
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateCancellationHandlingAsync_WithIgnoredCancellation_ReturnsFalse()
        {
            Task TestOperationAsync(CancellationToken ct)
            {
                return new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously).Task;
            }

            var result = await AsyncRules.ValidateCancellationHandlingAsync(TestOperationAsync, TimeSpan.FromMilliseconds(100));

            Assert.False(result);
        }

        [Fact]
        public async Task ValidateCancellationHandlingAsync_WhenOperationCompletesBeforeCancellation_ReturnsFalse()
        {
            var result = await AsyncRules.ValidateCancellationHandlingAsync(
                _ => Task.CompletedTask,
                TimeSpan.FromMilliseconds(10));

            Assert.False(result);
        }

        [Fact]
        public async Task ValidateCancellationHandlingAsync_WhenCancellationCausesUnrelatedFault_ReturnsFalse()
        {
            Task TestOperationAsync(CancellationToken cancellationToken)
            {
                var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                cancellationToken.Register(static state =>
                {
                    var completionSource = (TaskCompletionSource<object?>)state!;
                    completionSource.TrySetException(new InvalidOperationException("operation faulted"));
                }, completion);
                return completion.Task;
            }

            var result = await AsyncRules.ValidateCancellationHandlingAsync(TestOperationAsync, TimeSpan.FromMilliseconds(10));

            Assert.False(result);
        }

        [Fact]
        public async Task ValidateCancellationHandlingAsync_WhenOperationThrowsSynchronously_ReturnsFalse()
        {
            var result = await AsyncRules.ValidateCancellationHandlingAsync(
                _ => throw new InvalidOperationException("operation failed to start"),
                TimeSpan.FromMilliseconds(10));

            Assert.False(result);
        }

        [Fact]
        public void HasCancellationPropagation_WithCancellationToken_ReturnsTrue()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithCancellationAsync));

            // Act
            var result = AsyncRules.HasCancellationPropagation(method);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasCancellationPropagation_WithoutCancellationToken_ReturnsFalse()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithoutCancellationAsync));

            // Act
            var result = AsyncRules.HasCancellationPropagation(method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasSuspiciousAsyncNaming_DoesNotTreatAsyncSuffixAsSyncToken()
        {
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithCancellationAsync));

            Assert.False(AsyncRules.HasSuspiciousAsyncNaming(method));
        }

        [Theory]
        [InlineData(nameof(TestClass.MethodWithCancellationAsync))]
        [InlineData(nameof(TestClass.SynchronizeAsync))]
        [InlineData(nameof(TestClass.TrySyncWithPeerAsync))]
        [InlineData(nameof(TestClass.SyncOnRejoinAsync))]
        [InlineData(nameof(TestClass.MethodSyncAsync))]
        public void HasSuspiciousAsyncNaming_DoesNotTreatAsyncActionsAsSyncMethods(string methodName)
        {
            var method = typeof(TestClass).GetMethod(methodName);

            Assert.False(AsyncRules.HasSuspiciousAsyncNaming(method));
        }

        [Fact]
        public void HasSuspiciousAsyncNaming_FlagsTaskMethodEndingInSync()
        {
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodSync));

            Assert.True(AsyncRules.HasSuspiciousAsyncNaming(method));
        }

        [Fact]
        public void ScanMethod_WithSuspiciousNaming_ReturnsViolation()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodSync));

            // Act
            var violations = AsyncRules.ScanMethod(method).ToList();

            // Assert
            Assert.Single(violations);
            Assert.Equal(AsyncViolationType.SuspiciousNaming, violations[0].ViolationType);
            Assert.Equal(ViolationSeverity.Warning, violations[0].Severity);
        }

        [Fact]
        public void ScanAssembly_WithTestAssembly_ReturnsViolations()
        {
            // Arrange
            var assembly = typeof(TestClass).Assembly;

            // Act
            var violations = AsyncRules.ScanAssembly(assembly).ToList();

            // Assert
            // Should find at least the suspicious naming violation
            Assert.Contains(violations, v => v.ViolationType == AsyncViolationType.SuspiciousNaming);
        }

        [Fact]
        public void AnalyzerConfiguration_DisablesNameOnlyHeuristics()
        {
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("MissingParameterValidation"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("MissingCancellationToken"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("InefficientStringConcatenation"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("MutablePublicProperty"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("LargeClass"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("TooManyParameters"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("ExpensiveOperation"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("LoggingOnlyCatch"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("PotentialSqlInjection"));
            Assert.False(AnalyzerConfiguration.IsRuleEnabled("ExposesSensitiveData"));
        }

        /// <summary>
        ///     Test class with various method signatures for testing.
        /// </summary>
        private class TestClass
        {
            public async Task MethodWithCancellationAsync(CancellationToken ct)
            {
                await Task.Delay(1, ct);
            }

            public async Task MethodWithoutCancellationAsync()
            {
                await Task.Delay(1);
            }

            public Task SynchronizeAsync() => Task.CompletedTask;

            public Task TrySyncWithPeerAsync() => Task.CompletedTask;

            public Task SyncOnRejoinAsync() => Task.CompletedTask;

            public Task MethodSyncAsync() => Task.CompletedTask;

            public Task MethodSync() => Task.CompletedTask;
        }
    }
}
