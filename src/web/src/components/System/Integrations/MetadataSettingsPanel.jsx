// <copyright file="MetadataSettingsPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import React, { useEffect, useState } from 'react';
import * as YAML from 'yaml';
import * as optionsApi from '../../../lib/options';
import {
  Button,
  Card,
  Checkbox,
  Form,
  Header,
  Icon,
  Label,
  Message,
  Popup,
  Segment,
} from 'semantic-ui-react';


const getIntegrationsOptions = (options = {}) =>
  getOption(options, 'integration', 'Integration', 'integrations', 'Integrations') || {};

const getChromaprintOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'chromaprint', 'Chromaprint') || {};

const getAcoustIdOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'acoustId', 'acoustid', 'AcoustId') || {};

const getMusicBrainzOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'musicbrainz', 'MusicBrainz') || {};

const getLidarrOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'lidarr', 'Lidarr') || {};

const boolLabel = (value, trueText = 'Enabled', falseText = 'Disabled') => (
  <Label>
    <Label.Detail>
      <Icon color={value ? 'green' : 'grey'} name={value ? 'check' : 'close'} />
    </Label.Detail>
    {value ? trueText : falseText}
  </Label>
);

const isConfigured = (value) =>
  value !== undefined && value !== null && value !== '';

const toNumber = (value, fallback) => {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : fallback;
};

const chromaprintAlgorithmOptions = ['Default', 'Test1', 'Test2', 'Test3', 'Test4'].map(
  (value) => ({
    key: value,
    text: value,
    value,
  }),
);

const buildMetadataSettingsForm = (options = {}) => {
  const chromaprint = getChromaprintOptions(options);
  const acoustId = getAcoustIdOptions(options);
  const musicBrainz = getMusicBrainzOptions(options);
  const lidarrOptions = getLidarrOptions(options);

  return {
    acoustIdBaseUrl:
      getOption(acoustId, 'baseUrl', 'BaseUrl') || 'https://api.acoustid.org/v2',
    acoustIdClientId: '',
    acoustIdClientIdConfigured: isConfigured(
      getOption(acoustId, 'clientId', 'ClientId'),
    ),
    acoustIdEnabled: Boolean(getOption(acoustId, 'enabled', 'Enabled')),
    chromaprintAlgorithm:
      String(getOption(chromaprint, 'algorithm', 'Algorithm') || 'Default'),
    chromaprintChannels: String(getOption(chromaprint, 'channels', 'Channels') ?? 2),
    chromaprintDuration: String(
      getOption(chromaprint, 'durationSeconds', 'DurationSeconds') ?? 120,
    ),
    chromaprintEnabled: Boolean(getOption(chromaprint, 'enabled', 'Enabled')),
    chromaprintFfmpegPath:
      getOption(chromaprint, 'ffmpegPath', 'FfmpegPath') || 'ffmpeg',
    chromaprintSampleRate: String(
      getOption(chromaprint, 'sampleRate', 'SampleRate') ?? 44100,
    ),
    lidarrApiKey: '',
    lidarrApiKeyConfigured: isConfigured(
      getOption(lidarrOptions, 'apiKey', 'ApiKey'),
    ),
    lidarrAutoDownload: Boolean(
      getOption(lidarrOptions, 'autoDownload', 'AutoDownload'),
    ),
    lidarrAutoImportCompleted: Boolean(
      getOption(lidarrOptions, 'autoImportCompleted', 'AutoImportCompleted'),
    ),
    lidarrEnabled: Boolean(getOption(lidarrOptions, 'enabled', 'Enabled')),
    lidarrImportMode:
      getOption(lidarrOptions, 'importMode', 'ImportMode') || 'move',
    lidarrImportPathFrom:
      getOption(lidarrOptions, 'importPathFrom', 'ImportPathFrom') || '',
    lidarrImportPathTo:
      getOption(lidarrOptions, 'importPathTo', 'ImportPathTo') || '',
    lidarrImportReplaceExistingFiles: Boolean(
      getOption(
        lidarrOptions,
        'importReplaceExistingFiles',
        'ImportReplaceExistingFiles',
      ),
    ),
    lidarrMaxItemsPerSync: String(
      getOption(lidarrOptions, 'maxItemsPerSync', 'MaxItemsPerSync') ?? 100,
    ),
    lidarrSyncIntervalSeconds: String(
      getOption(lidarrOptions, 'syncIntervalSeconds', 'SyncIntervalSeconds') ??
        3600,
    ),
    lidarrSyncWantedToWishlist: Boolean(
      getOption(lidarrOptions, 'syncWantedToWishlist', 'SyncWantedToWishlist'),
    ),
    lidarrTimeoutSeconds: String(
      getOption(lidarrOptions, 'timeoutSeconds', 'TimeoutSeconds') ?? 20,
    ),
    lidarrUrl: getOption(lidarrOptions, 'url', 'Url') || '',
    lidarrWishlistFilter:
      getOption(lidarrOptions, 'wishlistFilter', 'WishlistFilter') || '',
    lidarrWishlistMaxResults: String(
      getOption(lidarrOptions, 'wishlistMaxResults', 'WishlistMaxResults') ??
        100,
    ),
    musicBrainzBaseUrl:
      getOption(musicBrainz, 'baseUrl', 'BaseUrl') ||
      'https://musicbrainz.org/ws/2',
    musicBrainzRetryAttempts: String(
      getOption(musicBrainz, 'retryAttempts', 'RetryAttempts') ?? 2,
    ),
    musicBrainzTimeoutSeconds: String(
      getOption(musicBrainz, 'timeoutSeconds', 'TimeoutSeconds') ?? 20,
    ),
    musicBrainzUserAgent: getOption(musicBrainz, 'userAgent', 'UserAgent') || '',
  };
};

const MetadataSettingsPanel = ({ options }) => {
  const remoteConfiguration = Boolean(
    getOption(options, 'remoteConfiguration', 'RemoteConfiguration'),
  );
  const [form, setForm] = useState(() => buildMetadataSettingsForm(options));
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);

  useEffect(() => {
    setForm(buildMetadataSettingsForm(options));
  }, [options]);

  const update = (key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const reset = () => {
    setForm(buildMetadataSettingsForm(options));
    setMessage(null);
  };

  const missingRequiredSettings = [
    form.chromaprintEnabled &&
      !form.chromaprintFfmpegPath.trim() &&
      'Chromaprint needs an ffmpeg executable path.',
    form.acoustIdEnabled &&
      !form.acoustIdClientIdConfigured &&
      !form.acoustIdClientId.trim() &&
      'AcoustID needs a client ID before fingerprint lookups can run.',
    form.lidarrEnabled &&
      !form.lidarrUrl.trim() &&
      'Lidarr needs an absolute base URL.',
    form.lidarrEnabled &&
      !form.lidarrApiKeyConfigured &&
      !form.lidarrApiKey.trim() &&
      'Lidarr needs an API key.',
    form.lidarrAutoImportCompleted &&
      Boolean(form.lidarrImportPathFrom.trim()) !==
        Boolean(form.lidarrImportPathTo.trim()) &&
      'Lidarr import path mapping needs both source and destination prefixes.',
  ].filter(Boolean);

  const buildPatch = () => {
    const lidarrPatch = {
      autoDownload: form.lidarrAutoDownload,
      autoImportCompleted: form.lidarrAutoImportCompleted,
      enabled: form.lidarrEnabled,
      importMode: form.lidarrImportMode,
      importPathFrom: form.lidarrImportPathFrom.trim(),
      importPathTo: form.lidarrImportPathTo.trim(),
      importReplaceExistingFiles: form.lidarrImportReplaceExistingFiles,
      maxItemsPerSync: toNumber(form.lidarrMaxItemsPerSync, 100),
      syncIntervalSeconds: toNumber(form.lidarrSyncIntervalSeconds, 3600),
      syncWantedToWishlist: form.lidarrSyncWantedToWishlist,
      timeoutSeconds: toNumber(form.lidarrTimeoutSeconds, 20),
      url: form.lidarrUrl.trim(),
      wishlistFilter: form.lidarrWishlistFilter.trim(),
      wishlistMaxResults: toNumber(form.lidarrWishlistMaxResults, 100),
    };

    if (form.lidarrApiKey.trim()) {
      lidarrPatch.apiKey = form.lidarrApiKey.trim();
    }

    const acoustIdPatch = {
      baseUrl: form.acoustIdBaseUrl.trim(),
      enabled: form.acoustIdEnabled,
    };

    if (form.acoustIdClientId.trim()) {
      acoustIdPatch.clientId = form.acoustIdClientId.trim();
    }

    return {
      acoustId: acoustIdPatch,
      chromaprint: {
        algorithm: form.chromaprintAlgorithm,
        channels: toNumber(form.chromaprintChannels, 2),
        durationSeconds: toNumber(form.chromaprintDuration, 120),
        enabled: form.chromaprintEnabled,
        ffmpegPath: form.chromaprintFfmpegPath.trim(),
        sampleRate: toNumber(form.chromaprintSampleRate, 44100),
      },
      lidarr: lidarrPatch,
      musicBrainz: {
        baseUrl: form.musicBrainzBaseUrl.trim(),
        retryAttempts: toNumber(form.musicBrainzRetryAttempts, 2),
        timeoutSeconds: Number.parseFloat(form.musicBrainzTimeoutSeconds) || 20,
        userAgent: form.musicBrainzUserAgent.trim(),
      },
    };
  };

  const markSecretsConfigured = (patch) => {
    setForm((current) => ({
      ...current,
      acoustIdClientId: '',
      acoustIdClientIdConfigured:
        current.acoustIdClientIdConfigured || Boolean(patch.acoustId.clientId),
      lidarrApiKey: '',
      lidarrApiKeyConfigured:
        current.lidarrApiKeyConfigured || Boolean(patch.lidarr.apiKey),
    }));
  };

  const saveYaml = async () => {
    setSaving(true);
    setMessage(null);
    const patch = buildPatch();

    try {
      const yaml = await optionsApi.getYaml();
      const document = YAML.parseDocument(yaml || '{}');
      const set = (path, value) => document.setIn(path, value);

      set(['integrations', 'chromaprint', 'enabled'], patch.chromaprint.enabled);
      set(['integrations', 'chromaprint', 'algorithm'], patch.chromaprint.algorithm);
      set(
        ['integrations', 'chromaprint', 'ffmpeg_path'],
        patch.chromaprint.ffmpegPath,
      );
      set(
        ['integrations', 'chromaprint', 'sample_rate'],
        patch.chromaprint.sampleRate,
      );
      set(['integrations', 'chromaprint', 'channels'], patch.chromaprint.channels);
      set(
        ['integrations', 'chromaprint', 'duration_seconds'],
        patch.chromaprint.durationSeconds,
      );

      set(['integrations', 'acoustid', 'enabled'], patch.acoustId.enabled);
      set(['integrations', 'acoustid', 'base_url'], patch.acoustId.baseUrl);
      if (patch.acoustId.clientId) {
        set(['integrations', 'acoustid', 'client_id'], patch.acoustId.clientId);
      }

      set(['integrations', 'musicbrainz', 'base_url'], patch.musicBrainz.baseUrl);
      set(
        ['integrations', 'musicbrainz', 'user_agent'],
        patch.musicBrainz.userAgent,
      );
      set(
        ['integrations', 'musicbrainz', 'timeout_seconds'],
        patch.musicBrainz.timeoutSeconds,
      );
      set(
        ['integrations', 'musicbrainz', 'retry_attempts'],
        patch.musicBrainz.retryAttempts,
      );

      set(['integrations', 'lidarr', 'enabled'], patch.lidarr.enabled);
      set(['integrations', 'lidarr', 'url'], patch.lidarr.url);
      if (patch.lidarr.apiKey) {
        set(['integrations', 'lidarr', 'api_key'], patch.lidarr.apiKey);
      }
      set(
        ['integrations', 'lidarr', 'timeout_seconds'],
        patch.lidarr.timeoutSeconds,
      );
      set(
        ['integrations', 'lidarr', 'sync_wanted_to_wishlist'],
        patch.lidarr.syncWantedToWishlist,
      );
      set(
        ['integrations', 'lidarr', 'sync_interval_seconds'],
        patch.lidarr.syncIntervalSeconds,
      );
      set(
        ['integrations', 'lidarr', 'max_items_per_sync'],
        patch.lidarr.maxItemsPerSync,
      );
      set(['integrations', 'lidarr', 'auto_download'], patch.lidarr.autoDownload);
      set(
        ['integrations', 'lidarr', 'wishlist_filter'],
        patch.lidarr.wishlistFilter,
      );
      set(
        ['integrations', 'lidarr', 'wishlist_max_results'],
        patch.lidarr.wishlistMaxResults,
      );
      set(
        ['integrations', 'lidarr', 'auto_import_completed'],
        patch.lidarr.autoImportCompleted,
      );
      set(
        ['integrations', 'lidarr', 'import_path_from'],
        patch.lidarr.importPathFrom,
      );
      set(
        ['integrations', 'lidarr', 'import_path_to'],
        patch.lidarr.importPathTo,
      );
      set(['integrations', 'lidarr', 'import_mode'], patch.lidarr.importMode);
      set(
        ['integrations', 'lidarr', 'import_replace_existing_files'],
        patch.lidarr.importReplaceExistingFiles,
      );

      await optionsApi.updateYaml({ yaml: document.toString() });
      markSecretsConfigured(patch);
      setMessage({
        positive: true,
        text: 'Metadata and Servarr integration settings saved to YAML.',
      });
    } catch (error) {
      setMessage({
        negative: true,
        text:
          error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Failed to save metadata integration settings.',
      });
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="settings" />
          Metadata and Servarr Settings
        </Card.Header>
        <Card.Meta>
          YAML-backed setup for fingerprint, metadata, and Lidarr integrations.
        </Card.Meta>
      </Card.Content>
      <Card.Content>
        <div className="integration-status-row">
          {boolLabel(form.chromaprintEnabled, 'Chromaprint On', 'Chromaprint Off')}
          {boolLabel(form.acoustIdEnabled, 'AcoustID On', 'AcoustID Off')}
          <Label>
            <Icon
              name={form.acoustIdClientIdConfigured ? 'key' : 'warning sign'}
            />
            AcoustID Client{' '}
            {form.acoustIdClientIdConfigured ? 'Configured' : 'Missing'}
          </Label>
          {boolLabel(form.lidarrEnabled, 'Lidarr On', 'Lidarr Off')}
          <Label>
            <Icon name={form.lidarrApiKeyConfigured ? 'key' : 'warning sign'} />
            Lidarr API Key {form.lidarrApiKeyConfigured ? 'Configured' : 'Missing'}
          </Label>
        </div>

        {!remoteConfiguration && (
          <Message
            info
            size="small"
          >
            Remote configuration is disabled. Enable it or edit YAML manually in
            the Options tab to change these integration settings.
          </Message>
        )}

        {message && (
          <Message
            negative={message.negative}
            positive={message.positive}
            size="small"
          >
            {message.text}
          </Message>
        )}
        {missingRequiredSettings.length > 0 && (
          <Message
            size="small"
            warning
          >
            <Message.List items={missingRequiredSettings} />
          </Message>
        )}

        <Form className="metadata-settings-form">
          <Segment>
            <Header as="h4">
              <Icon name="barcode" />
              Chromaprint
            </Header>
            <Popup
              content="Enable local audio fingerprint generation through ffmpeg and Chromaprint. This only changes YAML; scans still run through explicit feature workflows."
              trigger={
                <Checkbox
                  aria-label="Enable Chromaprint fingerprinting"
                  checked={form.chromaprintEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable Chromaprint"
                  onChange={(_, { checked }) =>
                    update('chromaprintEnabled', checked)
                  }
                  toggle
                />
              }
            />
            <Form.Group widths="equal">
              <Form.Select
                aria-label="Chromaprint algorithm"
                disabled={!remoteConfiguration || saving}
                label="Algorithm"
                onChange={(_, { value }) => update('chromaprintAlgorithm', value)}
                options={chromaprintAlgorithmOptions}
                value={form.chromaprintAlgorithm}
              />
              <Form.Input
                aria-label="Chromaprint ffmpeg path"
                disabled={!remoteConfiguration || saving}
                label="ffmpeg Path"
                onChange={(_, { value }) => update('chromaprintFfmpegPath', value)}
                value={form.chromaprintFfmpegPath}
              />
            </Form.Group>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Chromaprint sample rate"
                disabled={!remoteConfiguration || saving}
                label="Sample Rate"
                min={1}
                onChange={(_, { value }) => update('chromaprintSampleRate', value)}
                type="number"
                value={form.chromaprintSampleRate}
              />
              <Form.Input
                aria-label="Chromaprint channels"
                disabled={!remoteConfiguration || saving}
                label="Channels"
                min={1}
                onChange={(_, { value }) => update('chromaprintChannels', value)}
                type="number"
                value={form.chromaprintChannels}
              />
              <Form.Input
                aria-label="Chromaprint duration seconds"
                disabled={!remoteConfiguration || saving}
                label="Duration Seconds"
                min={1}
                onChange={(_, { value }) => update('chromaprintDuration', value)}
                type="number"
                value={form.chromaprintDuration}
              />
            </Form.Group>
          </Segment>

          <Segment>
            <Header as="h4">
              <Icon name="certificate" />
              AcoustID
            </Header>
            <Popup
              content="Enable AcoustID metadata lookup for existing fingerprint workflows. Saving this does not submit audio or contact AcoustID immediately."
              trigger={
                <Checkbox
                  aria-label="Enable AcoustID lookup"
                  checked={form.acoustIdEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable AcoustID"
                  onChange={(_, { checked }) => update('acoustIdEnabled', checked)}
                  toggle
                />
              }
            />
            <Form.Group widths="equal">
              <Form.Input
                aria-label="AcoustID client ID"
                disabled={!remoteConfiguration || saving}
                label="Client ID"
                onChange={(_, { value }) => update('acoustIdClientId', value)}
                placeholder={
                  form.acoustIdClientIdConfigured
                    ? 'Configured'
                    : 'AcoustID client ID'
                }
                type="password"
                value={form.acoustIdClientId}
              />
              <Form.Input
                aria-label="AcoustID base URL"
                disabled={!remoteConfiguration || saving}
                label="Base URL"
                onChange={(_, { value }) => update('acoustIdBaseUrl', value)}
                value={form.acoustIdBaseUrl}
              />
            </Form.Group>
          </Segment>

          <Segment>
            <Header as="h4">
              <Icon name="database" />
              MusicBrainz
            </Header>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="MusicBrainz base URL"
                disabled={!remoteConfiguration || saving}
                label="Base URL"
                onChange={(_, { value }) => update('musicBrainzBaseUrl', value)}
                value={form.musicBrainzBaseUrl}
              />
              <Form.Input
                aria-label="MusicBrainz user agent"
                disabled={!remoteConfiguration || saving}
                label="User Agent"
                onChange={(_, { value }) => update('musicBrainzUserAgent', value)}
                value={form.musicBrainzUserAgent}
              />
            </Form.Group>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="MusicBrainz timeout seconds"
                disabled={!remoteConfiguration || saving}
                label="Timeout Seconds"
                min={1}
                onChange={(_, { value }) =>
                  update('musicBrainzTimeoutSeconds', value)
                }
                type="number"
                value={form.musicBrainzTimeoutSeconds}
              />
              <Form.Input
                aria-label="MusicBrainz retry attempts"
                disabled={!remoteConfiguration || saving}
                label="Retry Attempts"
                min={1}
                onChange={(_, { value }) =>
                  update('musicBrainzRetryAttempts', value)
                }
                type="number"
                value={form.musicBrainzRetryAttempts}
              />
            </Form.Group>
          </Segment>

          <Segment>
            <Header as="h4">
              <Icon name="disk" />
              Lidarr
            </Header>
            <Popup
              content="Enable Lidarr wanted-album sync and completed-download import settings. Live sync still runs only from explicit Lidarr actions or configured schedulers."
              trigger={
                <Checkbox
                  aria-label="Enable Lidarr integration settings"
                  checked={form.lidarrEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable Lidarr"
                  onChange={(_, { checked }) => update('lidarrEnabled', checked)}
                  toggle
                />
              }
            />
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Lidarr base URL setting"
                disabled={!remoteConfiguration || saving}
                label="Base URL"
                onChange={(_, { value }) => update('lidarrUrl', value)}
                value={form.lidarrUrl}
              />
              <Form.Input
                aria-label="Lidarr API key setting"
                disabled={!remoteConfiguration || saving}
                label="API Key"
                onChange={(_, { value }) => update('lidarrApiKey', value)}
                placeholder={
                  form.lidarrApiKeyConfigured ? 'Configured' : 'Lidarr API key'
                }
                type="password"
                value={form.lidarrApiKey}
              />
              <Form.Input
                aria-label="Lidarr timeout seconds setting"
                disabled={!remoteConfiguration || saving}
                label="Timeout Seconds"
                min={1}
                onChange={(_, { value }) => update('lidarrTimeoutSeconds', value)}
                type="number"
                value={form.lidarrTimeoutSeconds}
              />
            </Form.Group>
            <Form.Group grouped>
              <Popup
                content="Sync Lidarr wanted albums into slskdN Wishlist review items."
                trigger={
                  <Checkbox
                    aria-label="Enable Lidarr wanted sync setting"
                    checked={form.lidarrSyncWantedToWishlist}
                    disabled={!remoteConfiguration || saving}
                    label="Sync wanted albums to Wishlist"
                    onChange={(_, { checked }) =>
                      update('lidarrSyncWantedToWishlist', checked)
                    }
                  />
                }
              />
              <Popup
                content="Allow Lidarr-created Wishlist items to auto-download according to Wishlist rules."
                trigger={
                  <Checkbox
                    aria-label="Enable Lidarr auto-download setting"
                    checked={form.lidarrAutoDownload}
                    disabled={!remoteConfiguration || saving}
                    label="Auto-download synced Wishlist items"
                    onChange={(_, { checked }) =>
                      update('lidarrAutoDownload', checked)
                    }
                  />
                }
              />
              <Popup
                content="Allow completed slskdN download directories to be sent to Lidarr manual import."
                trigger={
                  <Checkbox
                    aria-label="Enable Lidarr completed import setting"
                    checked={form.lidarrAutoImportCompleted}
                    disabled={!remoteConfiguration || saving}
                    label="Auto-import completed downloads"
                    onChange={(_, { checked }) =>
                      update('lidarrAutoImportCompleted', checked)
                    }
                  />
                }
              />
            </Form.Group>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Lidarr sync interval seconds setting"
                disabled={!remoteConfiguration || saving}
                label="Sync Interval Seconds"
                min={300}
                onChange={(_, { value }) =>
                  update('lidarrSyncIntervalSeconds', value)
                }
                type="number"
                value={form.lidarrSyncIntervalSeconds}
              />
              <Form.Input
                aria-label="Lidarr max items per sync setting"
                disabled={!remoteConfiguration || saving}
                label="Max Items Per Sync"
                min={1}
                onChange={(_, { value }) =>
                  update('lidarrMaxItemsPerSync', value)
                }
                type="number"
                value={form.lidarrMaxItemsPerSync}
              />
              <Form.Input
                aria-label="Lidarr wishlist max results setting"
                disabled={!remoteConfiguration || saving}
                label="Wishlist Max Results"
                min={10}
                onChange={(_, { value }) =>
                  update('lidarrWishlistMaxResults', value)
                }
                type="number"
                value={form.lidarrWishlistMaxResults}
              />
            </Form.Group>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Lidarr wishlist filter setting"
                disabled={!remoteConfiguration || saving}
                label="Wishlist Filter"
                onChange={(_, { value }) => update('lidarrWishlistFilter', value)}
                value={form.lidarrWishlistFilter}
              />
              <Form.Input
                aria-label="Lidarr import path from setting"
                disabled={!remoteConfiguration || saving}
                label="Import Path From"
                onChange={(_, { value }) => update('lidarrImportPathFrom', value)}
                value={form.lidarrImportPathFrom}
              />
              <Form.Input
                aria-label="Lidarr import path to setting"
                disabled={!remoteConfiguration || saving}
                label="Import Path To"
                onChange={(_, { value }) => update('lidarrImportPathTo', value)}
                value={form.lidarrImportPathTo}
              />
            </Form.Group>
            <Form.Group widths="equal">
              <Form.Select
                aria-label="Lidarr import mode setting"
                disabled={!remoteConfiguration || saving}
                label="Import Mode"
                onChange={(_, { value }) => update('lidarrImportMode', value)}
                options={[
                  { key: 'move', text: 'move', value: 'move' },
                  { key: 'copy', text: 'copy', value: 'copy' },
                ]}
                value={form.lidarrImportMode}
              />
              <Popup
                content="Allow Lidarr to replace existing files during completed-download import."
                trigger={
                  <Checkbox
                    aria-label="Enable Lidarr replace existing files setting"
                    checked={form.lidarrImportReplaceExistingFiles}
                    disabled={!remoteConfiguration || saving}
                    label="Replace existing files"
                    onChange={(_, { checked }) =>
                      update('lidarrImportReplaceExistingFiles', checked)
                    }
                  />
                }
              />
            </Form.Group>
          </Segment>
        </Form>

        <div className="integration-actions">
          <Popup
            content="Persist these YAML-only integration settings to the configuration file. This does not test credentials or start provider requests."
            trigger={
              <Button
                disabled={!remoteConfiguration || missingRequiredSettings.length > 0}
                icon
                labelPosition="left"
                loading={saving}
                onClick={saveYaml}
                primary
              >
                <Icon name="file alternate" />
                Save YAML
              </Button>
            }
          />
          <Popup
            content="Discard unsaved metadata and Servarr edits and restore the values currently reported by the daemon."
            trigger={
              <Button
                disabled={saving}
                icon
                labelPosition="left"
                onClick={reset}
              >
                <Icon name="undo" />
                Reset
              </Button>
            }
          />
        </div>
      </Card.Content>
    </Card>
  );
};

export default MetadataSettingsPanel;
