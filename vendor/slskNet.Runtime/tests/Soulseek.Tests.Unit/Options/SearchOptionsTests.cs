// <copyright file="SearchOptionsTests.cs" company="JP Dillingham">
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

    public class SearchOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data")]
        [InlineData(1, 1, true, 0, 0, 0, 1, true)]
        [InlineData(15000, 250, false, 1, 100, 10, 25000, false)]
        public void Instantiates_With_Defaults(
            int searchTimeout,
            int responseLimit,
            bool filterResponses,
            int minimumResponseFileCount,
            int maximumPeerQueueLength,
            int minimumPeerUploadSpeed,
            int fileLimit,
            bool removeSingleCharacterSearchTerms)
        {
            Func<SearchResponse, bool> responseFilter = _ => true;
            Func<File, bool> fileFilter = _ => true;
            Action<(SearchStates PreviousState, Search Search)> stateChanged = _ => { };
            Action<(Search Search, SearchResponse Response)> responseReceived = _ => { };

            var o = new SearchOptions(
                searchTimeout,
                responseLimit,
                filterResponses,
                minimumResponseFileCount,
                maximumPeerQueueLength,
                minimumPeerUploadSpeed,
                fileLimit,
                removeSingleCharacterSearchTerms,
                responseFilter,
                fileFilter,
                stateChanged,
                responseReceived);

            Assert.Equal(searchTimeout, o.SearchTimeout);
            Assert.Equal(responseLimit, o.ResponseLimit);
            Assert.Equal(filterResponses, o.FilterResponses);
            Assert.Equal(minimumResponseFileCount, o.MinimumResponseFileCount);
            Assert.Equal(maximumPeerQueueLength, o.MaximumPeerQueueLength);
            Assert.Equal(minimumPeerUploadSpeed, o.MinimumPeerUploadSpeed);
            Assert.Equal(responseFilter, o.ResponseFilter);
            Assert.Equal(fileLimit, o.FileLimit);
            Assert.Equal(removeSingleCharacterSearchTerms, o.RemoveSingleCharacterSearchTerms);
            Assert.Equal(fileFilter, o.FileFilter);
            Assert.Equal(stateChanged, o.StateChanged);
            Assert.Equal(responseReceived, o.ResponseReceived);
        }

        [Theory(DisplayName = "Throws on invalid scalar options")]
        [InlineData(0, 1, 0, 0, 0, 1)]
        [InlineData(1, 0, 0, 0, 0, 1)]
        [InlineData(1, 1, -1, 0, 0, 1)]
        [InlineData(1, 1, 0, -1, 0, 1)]
        [InlineData(1, 1, 0, 0, -1, 1)]
        [InlineData(1, 1, 0, 0, 0, 0)]
        public void Throws_On_Invalid_Scalar_Options(
            int searchTimeout,
            int responseLimit,
            int minimumResponseFileCount,
            int maximumPeerQueueLength,
            int minimumPeerUploadSpeed,
            int fileLimit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SearchOptions(
                searchTimeout: searchTimeout,
                responseLimit: responseLimit,
                minimumResponseFileCount: minimumResponseFileCount,
                maximumPeerQueueLength: maximumPeerQueueLength,
                minimumPeerUploadSpeed: minimumPeerUploadSpeed,
                fileLimit: fileLimit));
        }
    }
}
