// <copyright file="TokenBucketTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//     Copyright (c) slskdN Team.
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Xunit;

    public class TokenBucketTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given 0 count")]
        public void Throws_ArgumentOutOfRangeException_Given_0_Count()
        {
            var ex = Record.Exception(() => new TokenBucket(0, 1000));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given negative count")]
        public void Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            var ex = Record.Exception(() => new TokenBucket(-1, 1000));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given 0 interval")]
        public void Throws_ArgumentOutOfRangeException_Given_0_Interval()
        {
            var ex = Record.Exception(() => new TokenBucket(1000, 0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("interval", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given negative interval")]
        public void Throws_ArgumentOutOfRangeException_Given_Negative_Interval()
        {
            var ex = Record.Exception(() => new TokenBucket(1000, -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("interval", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Sets properties"), AutoData]
        public void Sets_Properties(int count, int interval)
        {
            using (var t = new TokenBucket(count, interval))
            {
                Assert.Equal(count, t.Capacity);
                Assert.Equal(interval, t.GetProperty<System.Timers.Timer>("Clock").Interval);
                Assert.Equal(count, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "SetCount")]
        [Fact(DisplayName = "SetCount throws ArgumentOutOfRangeException given 0 count")]
        public void SetCount_Throws_ArgumentOutOfRangeException_Given_0_Count()
        {
            using (var t = new TokenBucket(10, 1000))
            {
                var ex = Record.Exception(() => t.SetCapacity(0));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "SetCount")]
        [Fact(DisplayName = "SetCount throws ArgumentOutOfRangeException given negative count")]
        public void SetCount_Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            using (var t = new TokenBucket(10, 1000))
            {
                var ex = Record.Exception(() => t.SetCapacity(-1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "SetCapacity")]
        [Theory(DisplayName = "SetCapacity sets capacity"), AutoData]
        public void SetCapacity_Sets_Capacity(int count)
        {
            using (var t = new TokenBucket(10, 1000))
            {
                t.SetCapacity(count);

                Assert.Equal(count, t.Capacity);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync decrements count by requested count")]
        public async Task GetAsync_Decrements_Count_By_Requested_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns capacity if request exceeds capacity")]
        public async Task GetAsync_Returns_Capacity_If_Request_Exceeds_Capacity()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                int tokens = 0;
                var ex = await Record.ExceptionAsync(async() => tokens = await t.GetAsync(11));

                Assert.Null(ex);
                Assert.Equal(10, tokens);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns available tokens if request exceeds available count")]
        public async Task GetAsync_Returns_Available_Tokens_If_Request_Exceeds_Available_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                await t.GetAsync(6);
                var count = await t.GetAsync(6);

                Assert.Equal(4, count);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync throws ArgumentOutOfRangeException given zero count")]
        public async Task GetAsync_Throws_ArgumentOutOfRangeException_Given_Zero_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                var ex = await Record.ExceptionAsync(() => t.GetAsync(0));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("count", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync throws ArgumentOutOfRangeException given negative count")]
        public async Task GetAsync_Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                var ex = await Record.ExceptionAsync(() => t.GetAsync(-1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("count", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync waits for reset if bucket is depleted")]
        public async Task GetAsync_Waits_For_Reset_If_Bucket_Is_Depleted()
        {
            using (var t = new TokenBucket(1, 10))
            {
                await t.GetAsync(1);
                await t.GetAsync(1);
                await t.GetAsync(1);

                Assert.True(true);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync observes cancellation while waiting for reset")]
        public async Task GetAsync_Observes_Cancellation_While_Waiting_For_Reset()
        {
            using (var t = new TokenBucket(1, 1000000))
            using (var cts = new CancellationTokenSource())
            {
                await t.GetAsync(1);

                var task = t.GetAsync(1, cts.Token);
                await cts.CancelAsync();

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<TaskCanceledException>(ex);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "Dispose releases waiters waiting for reset")]
        public async Task Dispose_Releases_Waiters_Waiting_For_Reset()
        {
            var t = new TokenBucket(1, 1000000);
            await t.GetAsync(1);

            var task = t.GetAsync(1);
            t.Dispose();

            var ex = await Record.ExceptionAsync(() => task);

            Assert.NotNull(ex);
            Assert.IsType<ObjectDisposedException>(ex);
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return does not change count given negative")]
        public async Task Return_Does_Not_Change_Count_Given_Negative()
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));

                t.Return(-5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return adds capacity given value larger than capacity")]
        public async Task Return_Adds_Capacity_Given_Value_Larger_Than_Capacity()
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));

                t.Return(50);

                Assert.Equal(15, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return adds given value")]
        public async Task Return_Adds_Given_Value()
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));

                t.Return(5);

                Assert.Equal(10, t.GetProperty<long>("CurrentCount"));
            }
        }
    }
}
