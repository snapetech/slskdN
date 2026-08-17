// <copyright file="PeerObfuscationOptionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
// </copyright>

namespace Soulseek.Tests.Unit.Options
{
    using System;
    using Xunit;

    public class PeerObfuscationOptionsTests
    {
        [Fact(DisplayName = "Instantiation throws if enabled with unsupported type")]
        public void Instantiation_Throws_If_Enabled_With_Unsupported_Type()
        {
            var ex = Record.Exception(() => new PeerObfuscationOptions(enabled: true, listenPort: 24000, type: 2));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Fact(DisplayName = "Instantiation throws if regular port advertisement is disabled while enabled")]
        public void Instantiation_Throws_If_Regular_Port_Advertisement_Is_Disabled_While_Enabled()
        {
            var ex = Record.Exception(() => new PeerObfuscationOptions(enabled: true, listenPort: 24000, advertiseRegularPort: false));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact(DisplayName = "Instantiation does not throw if enabled with a zero (shared) listen port")]
        public void Instantiation_Does_Not_Throw_If_Enabled_With_Zero_Listen_Port()
        {
            var ex = Record.Exception(() => new PeerObfuscationOptions(enabled: true, listenPort: 0));

            Assert.Null(ex);
        }

        [Fact(DisplayName = "A zero listen port is retained as zero (shared) when enabled")]
        public void Zero_Listen_Port_Is_Retained_When_Enabled()
        {
            var options = new PeerObfuscationOptions(enabled: true, listenPort: 0);

            Assert.True(options.Enabled);
            Assert.Equal(0, options.ListenPort);
        }
    }
}
