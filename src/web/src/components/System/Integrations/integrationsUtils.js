// <copyright file="integrationsUtils.js" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

/**
 * Returns the first truthy value from the source object matching any of the provided keys.
 * @param {object} source
 * @param  {...string} keys
 */
export const getOption = (source, ...keys) => {
  for (const key of keys) {
    if (source && Object.prototype.hasOwnProperty.call(source, key)) {
      return source[key];
    }
  }
  return undefined;
};

export const getIntegrationsOptions = (options = {}) =>
  getOption(options, 'integration', 'Integration', 'integrations', 'Integrations') || {};

/** Shared boolean labeling component */
export const boolLabel = (value, trueText = 'Enabled', falseText = 'Disabled') => (
  <span className="label">{value ? trueText : falseText}</span>
);

/** Returns dash for empty values */
export const valueOrDash = (value) =>
  value === undefined || value === null || value === '' ? '-' : value;

/** Checks if a secret/token field has been configured */
export const isConfigured = (value) =>
  value !== undefined && value !== null && value !== '';

/** Safe number parser */
export const toNumber = (value, fallback) => {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : fallback;
};

/** VPN helpers */
export const getVpnOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'vpn', 'Vpn', 'VPN') || {};

export const getVpnState = (state = {}) => getOption(state, 'vpn', 'Vpn', 'VPN') || {};

export const portForwards = (vpn = {}) =>
  (Array.isArray(vpn) ? vpn : (getOption(vpn, 'portForwards', 'PortForwards') || []));

/** Lidarr helpers */
export const getLidarrOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'lidarr', 'Lidarr') || {};

/** Source Feed helpers omitted for brevity (Spotify/YouTube/LastFm) */
export const getSpotifyOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'spotify', 'Spotify') || {};

export const getYouTubeOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'youtube', 'YouTube', 'Youtube') || {};

export const getLastFmOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'lastfm', 'LastFm') || {};

/** Chromaprint / AcoustID / MusicBrainz helpers */
export const getChromaprintOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'chromaprint', 'Chromaprint') || {};

export const getAcoustIdOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'acoustId', 'acoustid', 'AcoustId') || {};

export const getMusicBrainzOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'musicbrainz', 'MusicBrainz') || {};

/** Pushbullet / Ntfy / Pushover helpers */
export const getPushbulletOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'pushbullet', 'Pushbullet') || {};

export const getNtfyOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'ntfy', 'Ntfy') || {};

export const getPushoverOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'pushover', 'Pushover') || {};

/** FTP helpers */
export const getFtpOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'ftp', 'Ftp', 'FTP') || {};
