// <copyright file="FtpIntegrationPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import React, { useEffect, useState } from 'react';
import * as YAML from 'yaml';
import * as optionsApi from '../../../lib/options';
import {
  Button,
  Card,
  Checkbox,
  Dropdown,
  Form,
  Header,
  Icon,
  Input,
  Label,
  Message,
  Popup,
  Segment,
} from 'semantic-ui-react';

const getOption = (source, ...keys) => {
  for (const key of keys) {
    if (source && Object.prototype.hasOwnProperty.call(source, key)) {
      return source[key];
    }
  }
  return undefined;
};


const getIntegrationsOptions = (options = {}) =>
  getOption(options, 'integration', 'Integration', 'integrations', 'Integrations') || {};

const getFtpOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'ftp', 'Ftp', 'FTP') || {};

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

const ftpEncryptionOptions = ['auto', 'none', 'implicit', 'explicit'].map((value) => ({
  key: value,
  text: value,
  value,
}));

const buildFtpForm = (options = {}) => {
  const ftp = getFtpOptions(options);

  return {
    address: getOption(ftp, 'address', 'Address') || '',
    connectionTimeout: String(
      getOption(ftp, 'connectionTimeout', 'ConnectionTimeout') ?? 5000,
    ),
    enabled: Boolean(getOption(ftp, 'enabled', 'Enabled')),
    encryptionMode:
      getOption(ftp, 'encryptionMode', 'EncryptionMode') || 'auto',
    ignoreCertificateErrors: Boolean(
      getOption(ftp, 'ignoreCertificateErrors', 'IgnoreCertificateErrors'),
    ),
    overwriteExisting:
      getOption(ftp, 'overwriteExisting', 'OverwriteExisting') ?? true,
    password: '',
    passwordConfigured: isConfigured(getOption(ftp, 'password', 'Password')),
    port: String(getOption(ftp, 'port', 'Port') ?? 21),
    remotePath: getOption(ftp, 'remotePath', 'RemotePath') || '/',
    retryAttempts: String(getOption(ftp, 'retryAttempts', 'RetryAttempts') ?? 3),
    username: getOption(ftp, 'username', 'Username') || '',
  };
};

const FtpIntegrationPanel = ({ options }) => {
  const remoteConfiguration = Boolean(
    getOption(options, 'remoteConfiguration', 'RemoteConfiguration'),
  );
  const [form, setForm] = useState(() => buildFtpForm(options));
  const [savingAction, setSavingAction] = useState('');
  const [message, setMessage] = useState(null);
  const saving = Boolean(savingAction);

  useEffect(() => {
    setForm(buildFtpForm(options));
  }, [options]);

  const update = (key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const reset = () => {
    setForm(buildFtpForm(options));
    setMessage(null);
  };

  const missingRequiredSettings = [
    form.enabled &&
      !form.address.trim() &&
      'FTP needs a server address before completed downloads can be uploaded.',
  ].filter(Boolean);

  const buildOverlay = () => {
    const ftpPatch = {
      address: form.address.trim(),
      connectionTimeout: toNumber(form.connectionTimeout, 5000),
      enabled: form.enabled,
      encryptionMode: form.encryptionMode,
      ignoreCertificateErrors: form.ignoreCertificateErrors,
      overwriteExisting: form.overwriteExisting,
      port: toNumber(form.port, 21),
      remotePath: form.remotePath.trim() || '/',
      retryAttempts: toNumber(form.retryAttempts, 3),
      username: form.username.trim(),
    };

    if (form.password.trim()) {
      ftpPatch.password = form.password.trim();
    }

    return {
      integration: {
        ftp: ftpPatch,
      },
    };
  };

  const markSecretsConfigured = (overlay) => {
    const ftpPatch = overlay.integration.ftp;

    setForm((current) => ({
      ...current,
      password: '',
      passwordConfigured: current.passwordConfigured || Boolean(ftpPatch.password),
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
        text: 'FTP integration settings applied for this running daemon.',
      });
    } catch (error) {
      setMessage({
        negative: true,
        text:
          error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Failed to apply FTP integration settings.',
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
      const ftpPatch = overlay.integration.ftp;

      set(['integrations', 'ftp', 'enabled'], ftpPatch.enabled);
      set(['integrations', 'ftp', 'address'], ftpPatch.address);
      set(['integrations', 'ftp', 'port'], ftpPatch.port);
      set(['integrations', 'ftp', 'username'], ftpPatch.username);
      if (ftpPatch.password) {
        set(['integrations', 'ftp', 'password'], ftpPatch.password);
      }

      set(['integrations', 'ftp', 'remote_path'], ftpPatch.remotePath);
      set(['integrations', 'ftp', 'encryption_mode'], ftpPatch.encryptionMode);
      set(
        ['integrations', 'ftp', 'ignore_certificate_errors'],
        ftpPatch.ignoreCertificateErrors,
      );
      set(
        ['integrations', 'ftp', 'overwrite_existing'],
        ftpPatch.overwriteExisting,
      );
      set(
        ['integrations', 'ftp', 'connection_timeout'],
        ftpPatch.connectionTimeout,
      );
      set(['integrations', 'ftp', 'retry_attempts'], ftpPatch.retryAttempts);

      await optionsApi.updateYaml({ yaml: document.toString() });
      markSecretsConfigured(overlay);
      setMessage({
        positive: true,
        text: 'FTP integration settings saved to YAML.',
      });
    } catch (error) {
      setMessage({
        negative: true,
        text:
          error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Failed to save FTP integration settings.',
      });
    } finally {
      setSavingAction('');
    }
  };

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="upload" />
          FTP Uploads
        </Card.Header>
        <Card.Meta>
          Completed-download upload target and connection policy.
        </Card.Meta>
      </Card.Content>
      <Card.Content>
        <div className="integration-status-row">
          {boolLabel(form.enabled, 'FTP On', 'FTP Off')}
          <Label>
            <Icon name={form.address ? 'server' : 'warning sign'} />
            Address {form.address ? 'Configured' : 'Missing'}
          </Label>
          <Label>
            <Icon name={form.passwordConfigured ? 'key' : 'lock'} />
            Password {form.passwordConfigured ? 'Configured' : 'Optional'}
          </Label>
          {boolLabel(
            form.overwriteExisting,
            'Overwrite Existing',
            'Skip Existing',
          )}
        </div>

        {!remoteConfiguration && (
          <Message
            info
            size="small"
          >
            Runtime configuration changes are disabled. Enable remote
            configuration or edit YAML in the Options tab to change FTP
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

        <Form className="ftp-settings-form">
          <Popup
            content="Turn on FTP uploads after completed downloads. This affects future completed-transfer handling."
            trigger={
              <Checkbox
                aria-label="Enable FTP completed-download uploads"
                checked={form.enabled}
                disabled={!remoteConfiguration || saving}
                label="Enable FTP uploads"
                onChange={(_, { checked }) => update('enabled', checked)}
                toggle
              />
            }
          />
          <Form.Group widths="equal">
            <Form.Input
              aria-label="FTP server address"
              disabled={!remoteConfiguration || saving}
              label="Server Address"
              onChange={(_, { value }) => update('address', value)}
              placeholder="ftp.example.net"
              value={form.address}
            />
            <Form.Input
              aria-label="FTP server port"
              disabled={!remoteConfiguration || saving}
              label="Port"
              max={65535}
              min={1}
              onChange={(_, { value }) => update('port', value)}
              type="number"
              value={form.port}
            />
            <Form.Select
              aria-label="FTP encryption mode"
              disabled={!remoteConfiguration || saving}
              label="Encryption"
              onChange={(_, { value }) => update('encryptionMode', value)}
              options={ftpEncryptionOptions}
              value={form.encryptionMode}
            />
          </Form.Group>
          <Form.Group widths="equal">
            <Form.Input
              aria-label="FTP username"
              disabled={!remoteConfiguration || saving}
              label="Username"
              onChange={(_, { value }) => update('username', value)}
              value={form.username}
            />
            <Form.Input
              aria-label="FTP password"
              disabled={!remoteConfiguration || saving}
              label="Password"
              onChange={(_, { value }) => update('password', value)}
              placeholder={form.passwordConfigured ? 'Configured' : 'Optional password'}
              type="password"
              value={form.password}
            />
          </Form.Group>
          <Form.Input
            aria-label="FTP remote upload path"
            disabled={!remoteConfiguration || saving}
            label="Remote Path"
            onChange={(_, { value }) => update('remotePath', value)}
            value={form.remotePath}
          />
          <Form.Group widths="equal">
            <Form.Input
              aria-label="FTP connection timeout milliseconds"
              disabled={!remoteConfiguration || saving}
              label="Connection Timeout Milliseconds"
              min={0}
              onChange={(_, { value }) => update('connectionTimeout', value)}
              type="number"
              value={form.connectionTimeout}
            />
            <Form.Input
              aria-label="FTP retry attempts"
              disabled={!remoteConfiguration || saving}
              label="Retry Attempts"
              max={5}
              min={0}
              onChange={(_, { value }) => update('retryAttempts', value)}
              type="number"
              value={form.retryAttempts}
            />
          </Form.Group>
          <Form.Group grouped>
            <Popup
              content="Overwrite files already present at the remote FTP path instead of skipping them."
              trigger={
                <Checkbox
                  aria-label="FTP overwrite existing remote files"
                  checked={form.overwriteExisting}
                  disabled={!remoteConfiguration || saving}
                  label="Overwrite existing remote files"
                  onChange={(_, { checked }) =>
                    update('overwriteExisting', checked)
                  }
                />
              }
            />
            <Popup
              content="Allow FTP certificate validation failures for self-signed or untrusted certificates."
              trigger={
                <Checkbox
                  aria-label="FTP ignore certificate errors"
                  checked={form.ignoreCertificateErrors}
                  disabled={!remoteConfiguration || saving}
                  label="Ignore certificate errors"
                  onChange={(_, { checked }) =>
                    update('ignoreCertificateErrors', checked)
                  }
                />
              }
            />
          </Form.Group>
        </Form>

        <div className="integration-actions">
          <Popup
            content="Apply these FTP settings through the runtime configuration overlay."
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
            content="Persist these FTP settings to the YAML configuration file."
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
            content="Discard unsaved FTP edits and restore the values currently reported by the daemon."
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


export default FtpIntegrationPanel;
