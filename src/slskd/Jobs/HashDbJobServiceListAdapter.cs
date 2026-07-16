// <copyright file="HashDbJobServiceListAdapter.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Jobs
{
    using System.Threading;
    using System.Threading.Tasks;
    using slskd.API.Native;
    using slskd.HashDb;

    /// <summary>
    ///     Production implementation of <see cref="IJobServiceWithList"/> backed by HashDb.
    ///     Prior to this, no production implementation was registered, so the GET /api/v0/jobs
    ///     endpoint always returned an empty list even when jobs existed — which is what the
    ///     System/Jobs Web UI renders as "doesn't load."
    /// </summary>
    public sealed class HashDbJobServiceListAdapter : IJobServiceWithList
    {
        private readonly IHashDbService hashDb;

        public HashDbJobServiceListAdapter(IHashDbService hashDb)
        {
            this.hashDb = hashDb;
        }

        public Task<JobListPage> GetJobListPageAsync(
            string? type,
            string? status,
            int limit,
            int offset,
            string? sortBy,
            bool descending,
            CancellationToken cancellationToken = default)
        {
            return hashDb.GetJobListPageAsync(
                type,
                status,
                limit,
                offset,
                sortBy,
                descending,
                cancellationToken);
        }
    }
}
