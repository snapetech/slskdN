// <copyright file="SourceFeedIntegrationsPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import { boolLabel, getAcoustIdOptions, getChromaprintOptions, getFtpOptions, getIntegrationsOptions, getLastFmOptions, getLidarrOptions, getMusicBrainzOptions, getNtfyOptions, getOption, getPushbulletOptions, getPushoverOptions, getSpotifyOptions, getVpnOptions, getVpnState, getYouTubeOptions, isConfigured, portForwards, toNumber, valueOrDash } from './integrationsUtils';

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
  Input,
  Label,
  Message,
  Popup,
  Segment,
} from 'semantic-ui-react';


const getIntegrationsOptions = (options = {}) =>
  getOption(options, 'integration', 'Integration', 'integrations', 'Integrations') || {};

const getSpotifyOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'spotify', 'Spotify') || {};

const getYouTubeOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'youtube', 'YouTube', 'Youtube') || {};

const getLastFmOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'lastfm', 'LastFm') || {};

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

const buildSourceFeedForm = (options = {}) => {
  const spotify = getSpotifyOptions(options);
  const youtube = getYouTubeOptions(options);
  const lastfm = getLastFmOptions(options);

  return {
    lastFmApiKey: '',
    lastFmConfigured: isConfigured(getOption(lastfm, 'apiKey', 'ApiKey')),
    lastFmEnabled: Boolean(getOption(lastfm, 'enabled', 'Enabled')),
    spotifyClientId: '',
    spotifyClientSecret: '',
    spotifyConfigured: isConfigured(getOption(spotify, 'clientId', 'ClientId')),
    spotifyEnabled: Boolean(getOption(spotify, 'enabled', 'Enabled')),
    spotifyMaxItems: String(
      getOption(spotify, 'maxItemsPerImport', 'MaxItemsPerImport') ?? 500,
    ),
    spotifyMarket: getOption(spotify, 'market', 'Market') || 'US',
    spotifyRedirectUri: getOption(spotify, 'redirectUri', 'RedirectUri') || '',
    spotifySecretConfigured: isConfigured(
      getOption(spotify, 'clientSecret', 'ClientSecret'),
    ),
    spotifyTargetPlaylist:
      getOption(spotify, 'targetPlaylist', 'TargetPlaylist') || '',
    youTubeApiKey: '',
    youTubeConfigured: isConfigured(getOption(youtube, 'apiKey', 'ApiKey')),
    youTubeEnabled: Boolean(getOption(youtube, 'enabled', 'Enabled')),
  };
};

const SourceFeedIntegrationsPanel = ({ options }) => {
  const remoteConfiguration = Boolean(
    getOption(options, 'remoteConfiguration', 'RemoteConfiguration'),
  );
  const [form, setForm] = useState(() => buildSourceFeedForm(options));
  const [savingAction, setSavingAction] = useState('');
  const [message, setMessage] = useState(null);
  const saving = Boolean(savingAction);

  useEffect(() => {
    setForm(buildSourceFeedForm(options));
  }, [options]);

  const update = (key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const reset = () => {
    setForm(buildSourceFeedForm(options));
    setMessage(null);
  };

  const missingRequiredSettings = [
    form.spotifyEnabled &&
      !form.spotifyConfigured &&
      !form.spotifyClientId.trim() &&
      'Spotify needs a client ID before account connection or provider imports can run.',
    form.youTubeEnabled &&
      !form.youTubeConfigured &&
      !form.youTubeApiKey.trim() &&
      'YouTube needs a Data API key before playlist expansion can run.',
    form.lastFmEnabled &&
      !form.lastFmConfigured &&
      !form.lastFmApiKey.trim() &&
      'Last.fm needs an API key before loved/recent/top imports can run.',
  ].filter(Boolean);
  const inferredSpotifyRedirectUri =
    form.spotifyRedirectUri.trim() ||
    (typeof window !== 'undefined'
      ? `${window.location.origin}/api/v0/integrations/spotify/callback`
      : '/api/v0/integrations/spotify/callback');

  const buildOverlay = () => {
    const spotifyPatch = {
      enabled: form.spotifyEnabled,
      maxItemsPerImport: toNumber(form.spotifyMaxItems, 500),
      market: form.spotifyMarket.trim().toUpperCase(),
      redirectUri: form.spotifyRedirectUri.trim(),
      timeoutSeconds: toNumber(form.spotifyTimeout, 20),
    };

    if (form.spotifyClientId.trim()) {
      spotifyPatch.clientId = form.spotifyClientId.trim();
    }

    if (form.spotifyClientSecret.trim()) {
      spotifyPatch.clientSecret = form.spotifyClientSecret.trim();
    }

    const youTubePatch = {
      enabled: form.youTubeEnabled,
    };

    if (form.youTubeApiKey.trim()) {
      youTubePatch.apiKey = form.youTubeApiKey.trim();
    }

    const lastFmPatch = {
      enabled: form.lastFmEnabled,
    };

    if (form.lastFmApiKey.trim()) {
      lastFmPatch.apiKey = form.lastFmApiKey.trim();
    }

    return {
      integration: {
        lastFm: lastFmPatch,
        spotify: spotifyPatch,
        youTube: youTubePatch,
      },
    };
  };

  const markSecretsConfigured = (overlay) => {
    const spotifyPatch = overlay.integration.spotify;
    const youTubePatch = overlay.integration.youTube;
    const lastFmPatch = overlay.integration.lastFm;

    setForm((current) => ({
      ...current,
      lastFmApiKey: '',
      lastFmConfigured: current.lastFmConfigured || Boolean(lastFmPatch.apiKey),
      spotifyClientId: '',
      spotifyClientSecret: '',
      spotifyConfigured: current.spotifyConfigured || Boolean(spotifyPatch.clientId),
      spotifySecretConfigured:
        current.spotifySecretConfigured || Boolean(spotifyPatch.clientSecret),
      youTubeApiKey: '',
      youTubeConfigured: current.youTubeConfigured || Boolean(youTubePatch.apiKey),
    }));
  };

  const applyRuntime = async () => {
    setSavingAction('runtime');
    setMessage(null);
    const overlay = buildOverlay();

    try {
      await optionsApi.applyOverlay(overlay);
      markSecretsConfigured(overlay);
      setMessage({
        positive: true,
        text: 'Source-feed integration settings applied for this running daemon.',
      });
    } catch (error) {
      setMessage({
        negative: true,
        text:
          error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Failed to apply source-feed integration settings.',
      });
    } finally {
      setSavingAction('');
    }
  };

  const saveYaml = async () => {
    setSavingAction('yaml');
    setMessage(null);
    const overlay = buildOverlay();

    try {
      const yaml = await optionsApi.getYaml();
      const document = YAML.parseDocument(yaml || '{}');
      const set = (path, value) => document.setIn(path, value);
      const spotifyPatch = overlay.integration.spotify;
      const youTubePatch = overlay.integration.youTube;
      const lastFmPatch = overlay.integration.lastFm;

      set(['integrations', 'spotify', 'enabled'], spotifyPatch.enabled);
      set(['integrations', 'spotify', 'redirect_uri'], spotifyPatch.redirectUri);
      set(['integrations', 'spotify', 'timeout_seconds'], spotifyPatch.timeoutSeconds);
      set(['integrations', 'spotify', 'max_items_per_import'], spotifyPatch.maxItemsPerImport);
      set(['integrations', 'spotify', 'market'], spotifyPatch.market);
      if (spotifyPatch.clientId) {
        set(['integrations', 'spotify', 'client_id'], spotifyPatch.clientId);
      }

      if (spotifyPatch.clientSecret) {
        set(['integrations', 'spotify', 'client_secret'], spotifyPatch.clientSecret);
      }

      set(['integrations', 'youtube', 'enabled'], youTubePatch.enabled);
      if (youTubePatch.apiKey) {
        set(['integrations', 'youtube', 'api_key'], youTubePatch.apiKey);
      }

      set(['integrations', 'lastfm', 'enabled'], lastFmPatch.enabled);
      if (lastFmPatch.apiKey) {
        set(['integrations', 'lastfm', 'api_key'], lastFmPatch.apiKey);
      }

      await optionsApi.updateYaml({ yaml: document.toString() });
      markSecretsConfigured(overlay);
      setMessage({
        positive: true,
        text: 'Source-feed integration settings saved to YAML.',
      });
    } catch (error) {
      setMessage({
        negative: true,
        text:
          error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Failed to save source-feed integration settings.',
      });
    } finally {
      setSavingAction('');
    }
  };

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="rss" />
          Source Feed Imports
        </Card.Header>
        <Card.Meta>
          Provider settings for Wishlist Import Feed previews.
        </Card.Meta>
      </Card.Content>
      <Card.Content>
        <div className="integration-status-row">
          {boolLabel(form.spotifyEnabled, 'Spotify On', 'Spotify Off')}
          <Label>
            <Icon name={form.spotifyConfigured ? 'key' : 'warning sign'} />
            Spotify Client ID {form.spotifyConfigured ? 'Configured' : 'Missing'}
          </Label>
          {boolLabel(form.youTubeEnabled, 'YouTube On', 'YouTube Off')}
          <Label>
            <Icon name={form.youTubeConfigured ? 'key' : 'warning sign'} />
            YouTube API Key {form.youTubeConfigured ? 'Configured' : 'Missing'}
          </Label>
          {boolLabel(form.lastFmEnabled, 'Last.fm On', 'Last.fm Off')}
          <Label>
            <Icon name={form.lastFmConfigured ? 'key' : 'warning sign'} />
            Last.fm API Key {form.lastFmConfigured ? 'Configured' : 'Missing'}
          </Label>
        </div>

        {!remoteConfiguration && (
          <Message
            info
            size="small"
          >
            Runtime configuration changes are disabled. Enable remote
            configuration or edit YAML in the Options tab to change these
            provider settings.
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

        <Form className="source-feed-settings-form">
          <Segment>
            <Header as="h4">
              <Icon name="spotify" />
              Spotify
            </Header>
            <Message
              info
              size="small"
            >
              <Message.Header>Spotify authorization redirect URI</Message.Header>
              Register this Redirect URI in the Spotify developer dashboard:{' '}
              <code>{inferredSpotifyRedirectUri}</code>
            </Message>
            <Popup
              content="Turns on Spotify source-feed imports and account connection. Private liked/saved/followed feeds still require a connected Spotify account or bearer token."
              trigger={
                <Checkbox
                  aria-label="Enable Spotify source-feed imports"
                  checked={form.spotifyEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable Spotify imports"
                  onChange={(_, { checked }) => update('spotifyEnabled', checked)}
                  toggle
                />
              }
            />
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Spotify client ID"
                disabled={!remoteConfiguration || saving}
                label="Client ID"
                onChange={(_, { value }) => update('spotifyClientId', value)}
                placeholder={form.spotifyConfigured ? 'Configured' : 'Spotify app client ID'}
                type="password"
                value={form.spotifyClientId}
              />
              <Form.Input
                aria-label="Spotify client secret"
                disabled={!remoteConfiguration || saving}
                label="Client Secret"
                onChange={(_, { value }) => update('spotifyClientSecret', value)}
                placeholder={
                  form.spotifySecretConfigured
                    ? 'Configured'
                    : 'Optional for OAuth; required for app-token public imports'
                }
                type="password"
                value={form.spotifyClientSecret}
              />
            </Form.Group>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Spotify redirect URI"
                disabled={!remoteConfiguration || saving}
                label="Redirect URI"
                onChange={(_, { value }) => update('spotifyRedirectUri', value)}
                placeholder={inferredSpotifyRedirectUri}
                value={form.spotifyRedirectUri}
              />
              <Form.Input
                aria-label="Spotify market"
                disabled={!remoteConfiguration || saving}
                label="Market"
                maxLength={2}
                onChange={(_, { value }) => update('spotifyMarket', value)}
                value={form.spotifyMarket}
              />
            </Form.Group>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Spotify timeout seconds"
                disabled={!remoteConfiguration || saving}
                label="Timeout Seconds"
                min={1}
                onChange={(_, { value }) => update('spotifyTimeout', value)}
                type="number"
                value={form.spotifyTimeout}
              />
              <Form.Input
                aria-label="Spotify max items per import"
                disabled={!remoteConfiguration || saving}
                label="Max Items Per Import"
                min={1}
                onChange={(_, { value }) => update('spotifyMaxItems', value)}
                type="number"
                value={form.spotifyMaxItems}
              />
            </Form.Group>
          </Segment>

          <Segment>
            <Header as="h4">
              <Icon name="youtube play" />
              YouTube
            </Header>
            <Popup
              content="Turns on YouTube Data API playlist expansion for explicitly previewed Import Feed URLs."
              trigger={
                <Checkbox
                  aria-label="Enable YouTube playlist source-feed imports"
                  checked={form.youTubeEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable YouTube playlist expansion"
                  onChange={(_, { checked }) => update('youTubeEnabled', checked)}
                  toggle
                />
              }
            />
            <Form.Input
              aria-label="YouTube Data API key"
              disabled={!remoteConfiguration || saving}
              label="API Key"
              onChange={(_, { value }) => update('youTubeApiKey', value)}
              placeholder={form.youTubeConfigured ? 'Configured' : 'YouTube Data API key'}
              type="password"
              value={form.youTubeApiKey}
            />
          </Segment>

          <Segment>
            <Header as="h4">
              <Icon name="lastfm" />
              Last.fm
            </Header>
            <Popup
              content="Turns on Last.fm API imports for explicitly previewed loved, recent, and top-track user URLs."
              trigger={
                <Checkbox
                  aria-label="Enable Last.fm source-feed imports"
                  checked={form.lastFmEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable Last.fm imports"
                  onChange={(_, { checked }) => update('lastFmEnabled', checked)}
                  toggle
                />
              }
            />
            <Form.Input
              aria-label="Last.fm API key"
              disabled={!remoteConfiguration || saving}
              label="API Key"
              onChange={(_, { value }) => update('lastFmApiKey', value)}
              placeholder={form.lastFmConfigured ? 'Configured' : 'Last.fm API key'}
              type="password"
              value={form.lastFmApiKey}
            />
          </Segment>
        </Form>

        <div className="integration-actions">
          <Popup
            content="Apply these source-feed integration settings through the runtime configuration overlay."
            trigger={
              <Button
                disabled={!remoteConfiguration || missingRequiredSettings.length > 0}
                icon
                labelPosition="left"
                loading={savingAction === 'runtime'}
                onClick={applyRuntime}
                primary
              >
                <Icon name="save" />
                Apply Runtime
              </Button>
            }
          />
          <Popup
            content="Persist these source-feed integration settings to the YAML configuration file."
            trigger={
              <Button
                disabled={!remoteConfiguration || missingRequiredSettings.length > 0}
                icon
                labelPosition="left"
                loading={savingAction === 'yaml'}
                onClick={saveYaml}
              >
                <Icon name="file alternate" />
                Save YAML
              </Button>
            }
          />
          <Popup
            content="Discard unsaved edits and restore the values currently reported by the daemon."
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


export default SourceFeedIntegrationsPanel;
