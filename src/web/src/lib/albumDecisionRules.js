// <copyright file="albumDecisionRules.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { getLocalStorageItem, setLocalStorageItem } from './storage';

const STORAGE_KEY = 'slskdn.albumDecisionRules';
const MAX_RULES = 50;

const normalizeText = (value = '') =>
  value
    .toLowerCase()
    .replace(/[^\d a-z]+/gu, ' ')
    .replace(/\s+/gu, ' ')
    .trim();

const asArray = (value) => (Array.isArray(value) ? value : []);

const parseRules = () => {
  try {
    const parsed = JSON.parse(getLocalStorageItem(STORAGE_KEY, '[]'));
    return Array.isArray(parsed)
      ? parsed.filter((rule) =>
        rule && typeof rule === 'object' && !Array.isArray(rule))
      : [];
  } catch {
    return [];
  }
};

export const getAlbumDecisionRules = () => parseRules();

export const buildAlbumDecisionRule = ({
  candidate,
  createdAt = new Date().toISOString(),
  searchText = '',
} = {}) => {
  const albumKey = normalizeText(candidate?.albumTitle || searchText);
  const searchKey = normalizeText(searchText);
  const formatMix = asArray(candidate?.formatMix)
    .filter((item) => item && typeof item === 'object' && !Array.isArray(item));
  const substitutionOptions = asArray(candidate?.substitutionOptions)
    .filter((option) => option && typeof option === 'object' && !Array.isArray(option));
  const warnings = asArray(candidate?.warnings);
  const formatPolicy = formatMix
    .map((item) => `${item.format}:${item.count}`)
    .join(',');

  return {
    albumKey,
    albumTitle: candidate?.albumTitle || searchText,
    createdAt,
    expectedTrackCount: candidate?.expectedTrackCount || 0,
    formatPolicy,
    id: `${albumKey || searchKey}:${candidate?.expectedTrackCount || 0}:${formatPolicy}`,
    minCompleteness: candidate?.completenessRatio || 0,
    notes: [
      ...warnings.map((warning) => `warn:${warning}`),
      ...substitutionOptions.map(
        (option) =>
          `substitute:track-${option.trackNumber}:${option.optionCount}-options`,
      ),
    ],
    searchKey,
    sourceCount: candidate?.sourceCount || 0,
    substitutionTracks: substitutionOptions.map(
      (option) => option.trackNumber,
    ),
    warningCount: warnings.length,
  };
};

export const saveAlbumDecisionRule = ({ candidate, searchText } = {}) => {
  const rule = buildAlbumDecisionRule({ candidate, searchText });
  const existing = parseRules().filter((item) => item.id !== rule.id);
  const rules = [rule, ...existing].slice(0, MAX_RULES);

  setLocalStorageItem(STORAGE_KEY, JSON.stringify(rules));

  return {
    rule,
    rules,
  };
};
