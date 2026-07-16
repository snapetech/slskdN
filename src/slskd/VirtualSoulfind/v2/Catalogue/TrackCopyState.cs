// <copyright file="TrackCopyState.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.v2.Catalogue;

public readonly record struct TrackCopyState(bool HasLocalFile, bool HasVerifiedCopy);
