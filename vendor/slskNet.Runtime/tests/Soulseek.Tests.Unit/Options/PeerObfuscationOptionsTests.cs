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
    }
}
