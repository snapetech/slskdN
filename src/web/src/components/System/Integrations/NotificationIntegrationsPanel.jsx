// <copyright file="NotificationIntegrationsPanel.js" company="slskdN Team">
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
  Label,
  Message,
  Popup,
  Segment,
} from 'semantic-ui-react';


const getIntegrationsOptions = (options = {}) =>
  getOption(options, 'integration', 'Integration', 'integrations', 'Integrations') ||
  {};

const getPushbulletOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'pushbullet', 'Pushbullet') || {};

const getNtfyOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'ntfy', 'Ntfy') || {};

const getPushoverOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'pushover', 'Pushover') || {};

const boolLabel = (value, trueText = 'Enabled', falseText = 'Disabled') => (
  <Label>
    <Label.Detail>
      <Icon
        color={value ? 'green' : 'grey'}
        name={value ? 'check' : 'close'}
      />
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

const buildNotificationForm = (options = {}) => {
  const pushbullet = getPushbulletOptions(options);
  const ntfy = getNtfyOptions(options);
  const pushover = getPushoverOptions(options);

  return {
    ntfyAccessToken: '',
    ntfyAccessTokenConfigured: isConfigured(
      getOption(ntfy, 'accessToken', 'AccessToken'),
    ),
    ntfyEnabled: Boolean(getOption(ntfy, 'enabled', 'Enabled')),
    ntfyNotifyOnPrivateMessage:
      getOption(ntfy, 'notifyOnPrivateMessage', 'NotifyOnPrivateMessage') ?? true,
    ntfyNotifyOnRoomMention:
      getOption(ntfy, 'notifyOnRoomMention', 'NotifyOnRoomMention') ?? true,
    ntfyPrefix: getOption(ntfy, 'notificationPrefix', 'NotificationPrefix') || 'slskdN',
    ntfyUrl: getOption(ntfy, 'url', 'Url') || '',
    pushbulletAccessToken: '',
    pushbulletAccessTokenConfigured: isConfigured(
      getOption(pushbullet, 'accessToken', 'AccessToken'),
    ),
    pushbulletCooldownTime: String(
      getOption(pushbullet, 'cooldownTime', 'CooldownTime') ?? 900000,
    ),
    pushbulletEnabled: Boolean(getOption(pushbullet, 'enabled', 'Enabled')),
    pushbulletNotifyOnPrivateMessage:
      getOption(
        pushbullet,
        'notifyOnPrivateMessage',
        'NotifyOnPrivateMessage',
      ) ?? true,
    pushbulletNotifyOnRoomMention:
      getOption(pushbullet, 'notifyOnRoomMention', 'NotifyOnRoomMention') ?? true,
    pushbulletPrefix:
      getOption(pushbullet, 'notificationPrefix', 'NotificationPrefix') ||
      'From slskdN:',
    pushbulletRetryAttempts: String(
      getOption(pushbullet, 'retryAttempts', 'RetryAttempts') ?? 3,
    ),
    pushoverEnabled: Boolean(getOption(pushover, 'enabled', 'Enabled')),
    pushoverNotifyOnPrivateMessage:
      getOption(
        pushover,
        'notifyOnPrivateMessage',
        'NotifyOnPrivateMessage',
      ) ?? true,
    pushoverNotifyOnRoomMention:
      getOption(pushover, 'notifyOnRoomMention', 'NotifyOnRoomMention') ?? true,
    pushoverPrefix:
      getOption(pushover, 'notificationPrefix', 'NotificationPrefix') || 'slskdN',
    pushoverToken: '',
    pushoverTokenConfigured: isConfigured(getOption(pushover, 'token', 'Token')),
    pushoverUserKey: '',
    pushoverUserKeyConfigured: isConfigured(getOption(pushover, 'userKey', 'UserKey')),
  };
};

const NotificationIntegrationsPanel = ({ options }) => {
  const remoteConfiguration = Boolean(
    getOption(options, 'remoteConfiguration', 'RemoteConfiguration'),
  );
  const [form, setForm] = useState(() => buildNotificationForm(options));
  const [savingAction, setSavingAction] = useState('');
  const [message, setMessage] = useState(null);
  const saving = Boolean(savingAction);

  useEffect(() => {
    setForm(buildNotificationForm(options));
  }, [options]);

  const update = (key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const reset = () => {
    setForm(buildNotificationForm(options));
    setMessage(null);
  };

  const missingRequiredSettings = [
    form.pushbulletEnabled &&
      !form.pushbulletAccessTokenConfigured &&
      !form.pushbulletAccessToken.trim() &&
      'Pushbullet needs an access token before notifications can be sent.',
    form.ntfyEnabled &&
      !form.ntfyUrl.trim() &&
      'Ntfy needs a topic URL before notifications can be sent.',
    form.pushoverEnabled &&
      !form.pushoverUserKeyConfigured &&
      !form.pushoverUserKey.trim() &&
      'Pushover needs a user key before notifications can be sent.',
    form.pushoverEnabled &&
      !form.pushoverTokenConfigured &&
      !form.pushoverToken.trim() &&
      'Pushover needs an API token before notifications can be sent.',
  ].filter(Boolean);

  const buildOverlay = () => {
    const pushbulletPatch = {
      cooldownTime: toNumber(form.pushbulletCooldownTime, 900000),
      enabled: form.pushbulletEnabled,
      notificationPrefix: form.pushbulletPrefix.trim(),
      notifyOnPrivateMessage: form.pushbulletNotifyOnPrivateMessage,
      notifyOnRoomMention: form.pushbulletNotifyOnRoomMention,
      retryAttempts: toNumber(form.pushbulletRetryAttempts, 3),
    };

    if (form.pushbulletAccessToken.trim()) {
      pushbulletPatch.accessToken = form.pushbulletAccessToken.trim();
    }

    const ntfyPatch = {
      enabled: form.ntfyEnabled,
      notificationPrefix: form.ntfyPrefix.trim(),
      notifyOnPrivateMessage: form.ntfyNotifyOnPrivateMessage,
      notifyOnRoomMention: form.ntfyNotifyOnRoomMention,
      url: form.ntfyUrl.trim(),
    };

    if (form.ntfyAccessToken.trim()) {
      ntfyPatch.accessToken = form.ntfyAccessToken.trim();
    }

    const pushoverPatch = {
      enabled: form.pushoverEnabled,
      notificationPrefix: form.pushoverPrefix.trim(),
      notifyOnPrivateMessage: form.pushoverNotifyOnPrivateMessage,
      notifyOnRoomMention: form.pushoverNotifyOnRoomMention,
    };

    if (form.pushoverUserKey.trim()) {
      pushoverPatch.userKey = form.pushoverUserKey.trim();
    }

    if (form.pushoverToken.trim()) {
      pushoverPatch.token = form.pushoverToken.trim();
    }

    return {
      integration: {
        ntfy: ntfyPatch,
        pushbullet: pushbulletPatch,
        pushover: pushoverPatch,
      },
    };
  };

  const markSecretsConfigured = (overlay) => {
    const pushbulletPatch = overlay.integration.pushbullet;
    const ntfyPatch = overlay.integration.ntfy;
    const pushoverPatch = overlay.integration.pushover;

    setForm((current) => ({
      ...current,
      ntfyAccessToken: '',
      ntfyAccessTokenConfigured:
        current.ntfyAccessTokenConfigured || Boolean(ntfyPatch.accessToken),
      pushbulletAccessToken: '',
      pushbulletAccessTokenConfigured:
        current.pushbulletAccessTokenConfigured ||
        Boolean(pushbulletPatch.accessToken),
      pushoverToken: '',
      pushoverTokenConfigured:
        current.pushoverTokenConfigured || Boolean(pushoverPatch.token),
      pushoverUserKey: '',
      pushoverUserKeyConfigured:
        current.pushoverUserKeyConfigured || Boolean(pushoverPatch.userKey),
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
        text: 'Notification integration settings applied for this running daemon.',
      });
    } catch (error) {
      setMessage({
        negative: true,
        text:
          error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Failed to apply notification integration settings.',
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
      const pushbulletPatch = overlay.integration.pushbullet;
      const ntfyPatch = overlay.integration.ntfy;
      const pushoverPatch = overlay.integration.pushover;

      set(['integrations', 'pushbullet', 'enabled'], pushbulletPatch.enabled);
      set(
        ['integrations', 'pushbullet', 'notification_prefix'],
        pushbulletPatch.notificationPrefix,
      );
      set(
        ['integrations', 'pushbullet', 'notify_on_private_message'],
        pushbulletPatch.notifyOnPrivateMessage,
      );
      set(
        ['integrations', 'pushbullet', 'notify_on_room_mention'],
        pushbulletPatch.notifyOnRoomMention,
      );
      set(
        ['integrations', 'pushbullet', 'retry_attempts'],
        pushbulletPatch.retryAttempts,
      );
      set(
        ['integrations', 'pushbullet', 'cooldown_time'],
        pushbulletPatch.cooldownTime,
      );
      if (pushbulletPatch.accessToken) {
        set(
          ['integrations', 'pushbullet', 'access_token'],
          pushbulletPatch.accessToken,
        );
      }

      set(['integrations', 'ntfy', 'enabled'], ntfyPatch.enabled);
      set(['integrations', 'ntfy', 'url'], ntfyPatch.url);
      set(
        ['integrations', 'ntfy', 'notification_prefix'],
        ntfyPatch.notificationPrefix,
      );
      set(
        ['integrations', 'ntfy', 'notify_on_private_message'],
        ntfyPatch.notifyOnPrivateMessage,
      );
      set(
        ['integrations', 'ntfy', 'notify_on_room_mention'],
        ntfyPatch.notifyOnRoomMention,
      );
      if (ntfyPatch.accessToken) {
        set(['integrations', 'ntfy', 'access_token'], ntfyPatch.accessToken);
      }

      set(['integrations', 'pushover', 'enabled'], pushoverPatch.enabled);
      set(
        ['integrations', 'pushover', 'notification_prefix'],
        pushoverPatch.notificationPrefix,
      );
      set(
        ['integrations', 'pushover', 'notify_on_private_message'],
        pushoverPatch.notifyOnPrivateMessage,
      );
      set(
        ['integrations', 'pushover', 'notify_on_room_mention'],
        pushoverPatch.notifyOnRoomMention,
      );
      if (pushoverPatch.userKey) {
        set(['integrations', 'pushover', 'user_key'], pushoverPatch.userKey);
      }

      if (pushoverPatch.token) {
        set(['integrations', 'pushover', 'token'], pushoverPatch.token);
      }

      await optionsApi.updateYaml({ yaml: document.toString() });
      markSecretsConfigured(overlay);
      setMessage({
        positive: true,
        text: 'Notification integration settings saved to YAML.',
      });
    } catch (error) {
      setMessage({
        negative: true,
        text:
          error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Failed to save notification integration settings.',
      });
    } finally {
      setSavingAction('');
    }
  };

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="bell" />
          Notifications
        </Card.Header>
        <Card.Meta>
          Push notifications for private messages and room mentions.
        </Card.Meta>
      </Card.Content>
      <Card.Content>
        <div className="integration-status-row">
          {boolLabel(form.pushbulletEnabled, 'Pushbullet On', 'Pushbullet Off')}
          <Label>
            <Icon
              name={form.pushbulletAccessTokenConfigured ? 'key' : 'warning sign'}
            />
            Pushbullet Token{' '}
            {form.pushbulletAccessTokenConfigured ? 'Configured' : 'Missing'}
          </Label>
          {boolLabel(form.ntfyEnabled, 'Ntfy On', 'Ntfy Off')}
          <Label>
            <Icon name={form.ntfyUrl ? 'linkify' : 'warning sign'} />
            Ntfy URL {form.ntfyUrl ? 'Configured' : 'Missing'}
          </Label>
          {boolLabel(form.pushoverEnabled, 'Pushover On', 'Pushover Off')}
          <Label>
            <Icon
              name={
                form.pushoverUserKeyConfigured && form.pushoverTokenConfigured
                  ? 'key'
                  : 'warning sign'
              }
            />
            Pushover Secrets{' '}
            {form.pushoverUserKeyConfigured && form.pushoverTokenConfigured
              ? 'Configured'
              : 'Missing'}
          </Label>
        </div>

        {!remoteConfiguration && (
          <Message
            info
            size="small"
          >
            Runtime configuration changes are disabled. Enable remote
            configuration or edit YAML in the Options tab to change notification
            settings.
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

        <Form className="notification-settings-form">
          <Segment>
            <Header as="h4">
              <Icon name="paper plane" />
              Pushbullet
            </Header>
            <Popup
              content="Turn on Pushbullet delivery for enabled notification triggers."
              trigger={
                <Checkbox
                  aria-label="Enable Pushbullet notifications"
                  checked={form.pushbulletEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable Pushbullet"
                  onChange={(_, { checked }) =>
                    update('pushbulletEnabled', checked)
                  }
                  toggle
                />
              }
            />
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Pushbullet access token"
                disabled={!remoteConfiguration || saving}
                label="Access Token"
                onChange={(_, { value }) => update('pushbulletAccessToken', value)}
                placeholder={
                  form.pushbulletAccessTokenConfigured
                    ? 'Configured'
                    : 'Pushbullet access token'
                }
                type="password"
                value={form.pushbulletAccessToken}
              />
              <Form.Input
                aria-label="Pushbullet notification prefix"
                disabled={!remoteConfiguration || saving}
                label="Title Prefix"
                onChange={(_, { value }) => update('pushbulletPrefix', value)}
                value={form.pushbulletPrefix}
              />
            </Form.Group>
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Pushbullet retry attempts"
                disabled={!remoteConfiguration || saving}
                label="Retry Attempts"
                max={5}
                min={0}
                onChange={(_, { value }) =>
                  update('pushbulletRetryAttempts', value)
                }
                type="number"
                value={form.pushbulletRetryAttempts}
              />
              <Form.Input
                aria-label="Pushbullet cooldown milliseconds"
                disabled={!remoteConfiguration || saving}
                label="Cooldown Milliseconds"
                min={0}
                onChange={(_, { value }) =>
                  update('pushbulletCooldownTime', value)
                }
                type="number"
                value={form.pushbulletCooldownTime}
              />
            </Form.Group>
            <Form.Group grouped>
              <Popup
                content="Send Pushbullet notifications when a private message arrives."
                trigger={
                  <Checkbox
                    aria-label="Pushbullet private message notifications"
                    checked={form.pushbulletNotifyOnPrivateMessage}
                    disabled={!remoteConfiguration || saving}
                    label="Private messages"
                    onChange={(_, { checked }) =>
                      update('pushbulletNotifyOnPrivateMessage', checked)
                    }
                  />
                }
              />
              <Popup
                content="Send Pushbullet notifications when your username is mentioned in a joined room."
                trigger={
                  <Checkbox
                    aria-label="Pushbullet room mention notifications"
                    checked={form.pushbulletNotifyOnRoomMention}
                    disabled={!remoteConfiguration || saving}
                    label="Room mentions"
                    onChange={(_, { checked }) =>
                      update('pushbulletNotifyOnRoomMention', checked)
                    }
                  />
                }
              />
            </Form.Group>
          </Segment>

          <Segment>
            <Header as="h4">
              <Icon name="rss" />
              Ntfy
            </Header>
            <Popup
              content="Turn on Ntfy delivery for enabled notification triggers."
              trigger={
                <Checkbox
                  aria-label="Enable Ntfy notifications"
                  checked={form.ntfyEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable Ntfy"
                  onChange={(_, { checked }) => update('ntfyEnabled', checked)}
                  toggle
                />
              }
            />
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Ntfy topic URL"
                disabled={!remoteConfiguration || saving}
                label="Topic URL"
                onChange={(_, { value }) => update('ntfyUrl', value)}
                placeholder="https://ntfy.sh/mytopic"
                value={form.ntfyUrl}
              />
              <Form.Input
                aria-label="Ntfy access token"
                disabled={!remoteConfiguration || saving}
                label="Access Token"
                onChange={(_, { value }) => update('ntfyAccessToken', value)}
                placeholder={
                  form.ntfyAccessTokenConfigured
                    ? 'Configured'
                    : 'Optional Ntfy token'
                }
                type="password"
                value={form.ntfyAccessToken}
              />
            </Form.Group>
            <Form.Input
              aria-label="Ntfy notification prefix"
              disabled={!remoteConfiguration || saving}
              label="Title Prefix"
              onChange={(_, { value }) => update('ntfyPrefix', value)}
              value={form.ntfyPrefix}
            />
            <Form.Group grouped>
              <Popup
                content="Send Ntfy notifications when a private message arrives."
                trigger={
                  <Checkbox
                    aria-label="Ntfy private message notifications"
                    checked={form.ntfyNotifyOnPrivateMessage}
                    disabled={!remoteConfiguration || saving}
                    label="Private messages"
                    onChange={(_, { checked }) =>
                      update('ntfyNotifyOnPrivateMessage', checked)
                    }
                  />
                }
              />
              <Popup
                content="Send Ntfy notifications when your username is mentioned in a joined room."
                trigger={
                  <Checkbox
                    aria-label="Ntfy room mention notifications"
                    checked={form.ntfyNotifyOnRoomMention}
                    disabled={!remoteConfiguration || saving}
                    label="Room mentions"
                    onChange={(_, { checked }) =>
                      update('ntfyNotifyOnRoomMention', checked)
                    }
                  />
                }
              />
            </Form.Group>
          </Segment>

          <Segment>
            <Header as="h4">
              <Icon name="mobile alternate" />
              Pushover
            </Header>
            <Popup
              content="Turn on Pushover delivery for enabled notification triggers."
              trigger={
                <Checkbox
                  aria-label="Enable Pushover notifications"
                  checked={form.pushoverEnabled}
                  disabled={!remoteConfiguration || saving}
                  label="Enable Pushover"
                  onChange={(_, { checked }) => update('pushoverEnabled', checked)}
                  toggle
                />
              }
            />
            <Form.Group widths="equal">
              <Form.Input
                aria-label="Pushover user key"
                disabled={!remoteConfiguration || saving}
                label="User Key"
                onChange={(_, { value }) => update('pushoverUserKey', value)}
                placeholder={
                  form.pushoverUserKeyConfigured
                    ? 'Configured'
                    : 'Pushover user key'
                }
                type="password"
                value={form.pushoverUserKey}
              />
              <Form.Input
                aria-label="Pushover API token"
                disabled={!remoteConfiguration || saving}
                label="API Token"
                onChange={(_, { value }) => update('pushoverToken', value)}
                placeholder={
                  form.pushoverTokenConfigured
                    ? 'Configured'
                    : 'Pushover application token'
                }
                type="password"
                value={form.pushoverToken}
              />
            </Form.Group>
            <Form.Input
              aria-label="Pushover notification prefix"
              disabled={!remoteConfiguration || saving}
              label="Title Prefix"
              onChange={(_, { value }) => update('pushoverPrefix', value)}
              value={form.pushoverPrefix}
            />
            <Form.Group grouped>
              <Popup
                content="Send Pushover notifications when a private message arrives."
                trigger={
                  <Checkbox
                    aria-label="Pushover private message notifications"
                    checked={form.pushoverNotifyOnPrivateMessage}
                    disabled={!remoteConfiguration || saving}
                    label="Private messages"
                    onChange={(_, { checked }) =>
                      update('pushoverNotifyOnPrivateMessage', checked)
                    }
                  />
                }
              />
              <Popup
                content="Send Pushover notifications when your username is mentioned in a joined room."
                trigger={
                  <Checkbox
                    aria-label="Pushover room mention notifications"
                    checked={form.pushoverNotifyOnRoomMention}
                    disabled={!remoteConfiguration || saving}
                    label="Room mentions"
                    onChange={(_, { checked }) =>
                      update('pushoverNotifyOnRoomMention', checked)
                    }
                  />
                }
              />
            </Form.Group>
          </Segment>
        </Form>

        <div className="integration-actions">
          <Popup
            content="Apply these notification settings through the runtime configuration overlay."
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
            content="Persist these notification settings to the YAML configuration file."
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
            content="Discard unsaved notification edits and restore the values currently reported by the daemon."
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

export default NotificationIntegrationsPanel;
