// <copyright file="opinions.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';

const baseUrl = '/opinions';

export const listOpinions = ({
  includeExpired = false,
  issuer,
  kind,
  limit = 100,
  scope,
  source,
  subjectId,
  subjectType,
} = {}) => {
  const params = {
    includeExpired,
    limit,
  };

  if (issuer) params.issuer = issuer;
  if (kind) params.kind = kind;
  if (scope) params.scope = scope;
  if (source) params.source = source;
  if (subjectId) params.subjectId = subjectId;
  if (subjectType) params.subjectType = subjectType;

  return api.get(baseUrl, { params });
};

export const getOpinionSummary = ({
  scope = 'global',
  subjectId,
  subjectType,
}) =>
  api.get(`${baseUrl}/summary`, {
    params: {
      scope,
      subjectId,
      subjectType,
    },
  });

export const submitOpinion = (opinion) => api.post(baseUrl, opinion);

export const deleteOpinion = (id) =>
  api.delete(`${baseUrl}/${encodeURIComponent(id)}`);
