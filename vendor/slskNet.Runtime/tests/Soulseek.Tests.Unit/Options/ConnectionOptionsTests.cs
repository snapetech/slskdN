// <copyright file="ConnectionOptionsTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit.Options
{
    using System;
    using Xunit;

    public class ConnectionOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates properly")]
        [InlineData(1, 1, 1, 0, -1)]
        [InlineData(16384, 16384, 250, 10000, 15000)]
        public void Instantiates_Properly(int read, int write, int depth, int timeout, int inactivity)
        {
            ConnectionOptions o = null;

            var ex = Record.Exception(() => o = new ConnectionOptions(read, write, depth, timeout, inactivity));

            Assert.Null(ex);
            Assert.NotNull(o);

            Assert.Equal(read, o.ReadBufferSize);
            Assert.Equal(write, o.WriteBufferSize);
            Assert.Equal(depth, o.WriteQueueSize);
            Assert.Equal(timeout, o.ConnectTimeout);
            Assert.Equal(inactivity, o.InactivityTimeout);
        }

        [Theory(DisplayName = "Throws on invalid scalar options")]
        [InlineData(0, 1, 1, 1, 1)]
        [InlineData(1, 0, 1, 1, 1)]
        [InlineData(1, 1, 0, 1, 1)]
        [InlineData(1, 1, 1, -1, 1)]
        [InlineData(1, 1, 1, 1, -2)]
        [InlineData(1, 1, 1, 1, 0)]
        public void Throws_On_Invalid_Scalar_Options(int read, int write, int depth, int timeout, int inactivity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConnectionOptions(read, write, depth, timeout, inactivity));
        }

        [Trait("Category", "WithoutInactivityTimeout")]
        [Fact(DisplayName = "WithoutInactivityTimeout forces InactivityTimeout to -1")]
        public void WithoutInactivityTimeout()
        {
            var o = new ConnectionOptions(inactivityTimeout: 5000);

            var o2 = o.WithoutInactivityTimeout();

            Assert.Equal(-1, o2.InactivityTimeout);
        }
    }
}
