// <copyright file="LidarrPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import { boolLabel, getAcoustIdOptions, getChromaprintOptions, getFtpOptions, getIntegrationsOptions, getLastFmOptions, getLidarrOptions, getMusicBrainzOptions, getNtfyOptions, getOption, getPushbulletOptions, getPushoverOptions, getSpotifyOptions, getVpnOptions, getVpnState, getYouTubeOptions, isConfigured, portForwards, toNumber, valueOrDash } from './integrationsUtils';

import React, { useState, useMemo } from 'react';
import {
  getStatus as lidarrGetStatus,
  getWanted as lidarrGetWanted,
  syncWanted as lidarrSyncWanted,
  importDirectory as lidarrImportDirectory,
  importCompletedDirectory as lidarrImportCompletedDirectory,
} from '../../../lib/lidarr';

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
  Table,
} from 'semantic-ui-react';


const getIntegrationsOptions = (options = {}) =>
  getOption(options, 'integration', 'Integration', 'integrations', 'Integrations') || {};

const getLidarrOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'lidarr', 'Lidarr') || {};

const isConfigured = (value) =>
  value !== undefined && value !== null && value !== '';

const boolLabel = (value, trueText = 'Enabled', falseText = 'Disabled') => (
  <Label>
    <Label.Detail>
      <Icon color={value ? 'green' : 'grey'} name={value ? 'check' : 'close'} />
    </Label.Detail>
    {value ? trueText : falseText}
  </Label>
);

const valueOrDash = (value) =>
  value === undefined || value === null || value === '' ? '-' : value;

const LidarrPanel = ({ options }) => {
  const lidarrOptions = getLidarrOptions(options);
  const [status, setStatus] = useState(null);
  const [wanted, setWanted] = useState([]);
  const [syncResult, setSyncResult] = useState(null);
  const [importDirectory, setImportDirectory] = useState('');
  const [importResult, setImportResult] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState('');
  const enabled = getOption(lidarrOptions, 'enabled', 'Enabled');

  const maskedApiKey = useMemo(() => {
    const apiKey = getOption(lidarrOptions, 'apiKey', 'ApiKey');
    return apiKey ? 'Configured' : 'Not configured';
  }, [lidarrOptions]);

  const run = async (name, action) => {
    setLoading(name);
    setError('');

    try {
      await action();
    } catch (error) {
      setError(
        error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Lidarr request failed',
      );
    } finally {
      setLoading('');
    }
  };

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="music" />
          Lidarr
        </Card.Header>
        <Card.Meta>Wanted-album sync and completed-download import bridge.</Card.Meta>
      </Card.Content>
      <Card.Content>
        <div className="integration-status-row">
          {boolLabel(enabled)}
          {boolLabel(
            getOption(lidarrOptions, 'syncWantedToWishlist', 'SyncWantedToWishlist'),
            'Wanted Sync',
            'Wanted Sync Off',
          )}
          {boolLabel(
            getOption(lidarrOptions, 'autoImportCompleted', 'AutoImportCompleted'),
            'Auto Import',
            'Auto Import Off',
          )}
          <Label>
            <Icon name={maskedApiKey === 'Configured' ? 'key' : 'warning sign'} />
            API Key {maskedApiKey}
          </Label>
        </div>
        <Table
          basic="very"
          compact
          definition
        >
          <Table.Body>
            <Table.Row>
              <Table.Cell>URL</Table.Cell>
              <Table.Cell>{valueOrDash(getOption(lidarrOptions, 'url', 'Url'))}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Timeout</Table.Cell>
              <Table.Cell>
                {valueOrDash(getOption(lidarrOptions, 'timeoutSeconds', 'TimeoutSeconds'))}
                {' s'}
              </Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Sync Interval</Table.Cell>
              <Table.Cell>
                {valueOrDash(getOption(lidarrOptions, 'syncIntervalSeconds', 'SyncIntervalSeconds'))}
                {' s'}
              </Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Import Mode</Table.Cell>
              <Table.Cell>{valueOrDash(getOption(lidarrOptions, 'importMode', 'ImportMode'))}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Import Path Map</Table.Cell>
              <Table.Cell>
                {valueOrDash(getOption(lidarrOptions, 'importPathFrom', 'ImportPathFrom'))}
                {' -> '}
                {valueOrDash(getOption(lidarrOptions, 'importPathTo', 'ImportPathTo'))}
              </Table.Cell>
            </Table.Row>
          </Table.Body>
        </Table>
        {error && (
          <Message
            negative
            size="small"
          >
            {error}
          </Message>
        )}
        <div className="integration-actions">
          <Popup
            content="Fetch Lidarr system status using the configured URL and API key."
            trigger={
              <Button
                icon
                labelPosition="left"
                loading={loading === 'status'}
                onClick={() =>
                  run('status', async () => setStatus(await lidarrGetStatus()))
                }
              >
                <Icon name="heartbeat" />
                Check Status
              </Button>
            }
          />
          <Popup
            content="Preview Lidarr wanted albums that can be synced into slskdN Wishlist."
            trigger={
              <Button
                icon
                labelPosition="left"
                loading={loading === 'wanted'}
                onClick={() =>
                  run('wanted', async () =>
                    setWanted(await lidarrGetWantedMissing({ pageSize: 25 })),
                  )
                }
              >
                <Icon name="list" />
                Load Wanted
              </Button>
            }
          />
          <Popup
            content="Create or refresh slskdN Wishlist entries from Lidarr wanted albums."
            trigger={
              <Button
                icon
                labelPosition="left"
                loading={loading === 'sync'}
                onClick={() =>
                  run('sync', async () => setSyncResult(await lidarrSyncWanted()))
                }
                primary
              >
                <Icon name="sync" />
                Sync Wanted
              </Button>
            }
          />
        </div>
        {status && (
          <Message
            positive
            size="small"
          >
            Lidarr responded: {status.appName || status.AppName || 'Lidarr'}{' '}
            {status.version || status.Version || ''}
          </Message>
        )}
        {syncResult && (
          <Message
            info
            size="small"
          >
            Wanted sync: {syncResult.createdCount ?? syncResult.CreatedCount ?? 0} created,{' '}
            {syncResult.duplicateCount ?? syncResult.DuplicateCount ?? 0} duplicates,{' '}
            {syncResult.skippedCount ?? syncResult.SkippedCount ?? 0} skipped.
          </Message>
        )}
        {wanted.length > 0 && (
          <Table
            celled
            compact
          >
            <Table.Header>
              <Table.Row>
                <Table.HeaderCell>Artist</Table.HeaderCell>
                <Table.HeaderCell>Album</Table.HeaderCell>
              </Table.Row>
            </Table.Header>
            <Table.Body>
              {wanted.slice(0, 10).map((album) => (
                <Table.Row key={album.id || album.Id || `${album.title}-${album.foreignAlbumId}`}>
                  <Table.Cell>
                    {album.artist?.artistName || album.Artist?.ArtistName || '-'}
                  </Table.Cell>
                  <Table.Cell>{album.title || album.Title || '-'}</Table.Cell>
                </Table.Row>
              ))}
            </Table.Body>
          </Table>
        )}
        <Segment className="integration-manual-import">
          <Header as="h4">Manual Import</Header>
          <Input
            action={{
              content: 'Import',
              disabled: !importDirectory.trim(),
              icon: 'download',
              loading: loading === 'import',
              onClick: () =>
                run('import', async () =>
                  setImportResult(
                    await lidarrImportCompletedDirectory({
                      directory: importDirectory.trim(),
                    }),
                  ),
                ),
            }}
            fluid
            onChange={(_, { value }) => setImportDirectory(value)}
            placeholder="Completed download directory visible to slskdN..."
            value={importDirectory}
          />
          {importResult && (
            <Message
              size="small"
              warning={Boolean(importResult.skippedReason || importResult.SkippedReason)}
            >
              {importResult.skippedReason || importResult.SkippedReason
                ? `Skipped: ${importResult.skippedReason || importResult.SkippedReason}`
                : `Queued Lidarr command ${importResult.commandId || importResult.CommandId || '-'}`}
            </Message>
          )}
        </Segment>
      </Card.Content>
    </Card>
  );
};



export default LidarrPanel;
