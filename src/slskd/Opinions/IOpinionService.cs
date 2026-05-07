// <copyright file="IOpinionService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Opinions;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soulseek;

public interface IOpinionService
{
    Task<OpinionRecord> SubmitAsync(OpinionRecord opinion, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpinionRecord>> ListAsync(OpinionQuery query, CancellationToken cancellationToken = default);

    Task<OpinionSummary> SummarizeAsync(
        OpinionSubjectType subjectType,
        string subjectId,
        string scope = "global",
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpinionRecord>> ImportSoulseekInterestsAsync(
        string username,
        UserInterests interests,
        CancellationToken cancellationToken = default);

    OpinionValidationResult Validate(OpinionRecord opinion);
}
