// <copyright file="SongIdServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using slskd.SongID;

public static class SongIdServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdSongId(this IServiceCollection services)
    {
        services.AddSingleton<ISongIdRunStore, SongIdRunStore>();
        services.AddSingleton<ISongIdCapabilityReporter, SongIdCapabilityReporter>();
        services.AddSingleton<ISongIdService, SongIdService>();

        return services;
    }
}
