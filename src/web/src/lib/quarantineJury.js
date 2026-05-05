// <copyright file="quarantineJury.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';

const baseUrl = '/quarantine-jury';

export const getRequests = async () =>
  {
    const { data } = await api.get(`${baseUrl}/requests`);
    return Array.isArray(data) ? data : [];
  };

export const getReview = async (requestId) =>
  (await api.get(`${baseUrl}/requests/${encodeURIComponent(requestId)}/review`))
    .data;

export const acceptReleaseCandidate = async (requestId, request = {}) =>
  (
    await api.post(
      `${baseUrl}/requests/${encodeURIComponent(requestId)}/accept-release-candidate`,
      request,
    )
  ).data;

export const routeRequest = async (requestId, request = {}) =>
  (
    await api.post(
      `${baseUrl}/requests/${encodeURIComponent(requestId)}/routes`,
      request,
    )
  ).data;
