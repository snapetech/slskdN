// <copyright file="ServarrReadinessPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import React, { useState, useMemo } from 'react';
import {
  buildServarrCompatibilityPreview,
  buildServarrReadiness,
  formatServarrCompatibilityReport,
  summarizeServarrReadiness,
} from '../../../lib/servarrReadiness';
import {
  Button,
  Card,
  Form,
  Header,
  Icon,
  Input,
  Message,
  Segment,
  Table,
  Label,
} from 'semantic-ui-react';

const getOption = (source, ...keys) => {
  for (const key of keys) {
    if (source && Object.prototype.hasOwnProperty.call(source, key)) {
      return source[key];
    }
  }
  return undefined;
};

const boolLabel = (value, trueText = 'Enabled', falseText = 'Disabled') => (
  <Label>
    <Label.Detail>
      <Icon color={value ? 'green' : 'grey'} name={value ? 'check' : 'close'} />
    </Label.Detail>
    {value ? trueText : falseText}
  </Label>
);


const getIntegrationsOptions = (options = {}) =>
  getOption(options, 'integration', 'Integration', 'integrations', 'Integrations') || {};

const getLidarrOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'lidarr', 'Lidarr') || {};

const valueOrDash = (value) =>
  value === undefined || value === null || value === '' ? '-' : value;

const ServarrReadinessPanel = ({ options }) => {
const valueOrDash = (value) =>
  value === undefined || value === null || value === '' ? '-' : value;

const ServarrReadinessPanel = ({ options }) => {
  const lidarrOptions = getLidarrOptions(options);
  const [copyStatus, setCopyStatus] = useState('');
  const [running, setRunning] = useState(false);
  const checks = buildServarrReadiness({
    apiKey: getOption(lidarrOptions, 'apiKey', 'ApiKey'),
    autoImportCompleted: getOption(
      lidarrOptions,
      'autoImportCompleted',
      'AutoImportCompleted',
    ),
    enabled: getOption(lidarrOptions, 'enabled', 'Enabled'),
    importPathFrom: getOption(lidarrOptions, 'importPathFrom', 'ImportPathFrom'),
    importPathTo: getOption(lidarrOptions, 'importPathTo', 'ImportPathTo'),
    syncWantedToWishlist: getOption(
      lidarrOptions,
      'syncWantedToWishlist',
      'SyncWantedToWishlist',
    ),
    url: getOption(lidarrOptions, 'url', 'Url'),
  });
  const summary = summarizeServarrReadiness(checks);
  const compatibility = buildServarrCompatibilityPreview({
    apiKey: getOption(lidarrOptions, 'apiKey', 'ApiKey'),
    autoImportCompleted: getOption(
      lidarrOptions,
      'autoImportCompleted',
      'AutoImportCompleted',
    ),
    enabled: getOption(lidarrOptions, 'enabled', 'Enabled'),
    importMode: getOption(lidarrOptions, 'importMode', 'ImportMode') || 'copy',
    importPathFrom: getOption(lidarrOptions, 'importPathFrom', 'ImportPathFrom'),
    importPathTo: getOption(lidarrOptions, 'importPathTo', 'ImportPathTo'),
    syncWantedToWishlist: getOption(
      lidarrOptions,
      'syncWantedToWishlist',
      'SyncWantedToWishlist',
    ),
    url: getOption(lidarrOptions, 'url', 'Url'),
  });

  const copyCompatibilityReport = async () => {
    const report = formatServarrCompatibilityReport(compatibility);
    if (!navigator.clipboard?.writeText) {
      setCopyStatus('Clipboard unavailable; copy the Servarr review manually.');
      return;
    }

    try {
      await navigator.clipboard.writeText(report);
      setCopyStatus('Servarr compatibility review copied.');
    } catch {
      setCopyStatus('Unable to copy Servarr compatibility review.');
    }
  };

  const runReadyActions = async () => {
    setRunning(true);
    setCopyStatus('');

    try {
      if (!compatibility.supportsWantedPull) {
        setCopyStatus('Wanted pull is not ready; no Servarr action was run.');
        return;
      }

      const result = await lidarr.syncWanted();
      setCopyStatus(
        `Wanted sync ran: ${result.createdCount ?? result.CreatedCount ?? 0} created, ${
          result.duplicateCount ?? result.DuplicateCount ?? 0
        } duplicates, ${result.skippedCount ?? result.SkippedCount ?? 0} skipped.`,
      );
    } catch (error) {
      setCopyStatus(
        error?.response?.data ||
          error?.response?.statusText ||
          error?.message ||
          'Servarr action failed.',
      );
    } finally {
      setRunning(false);
    }
  };

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="settings" />
          Servarr Setup
        </Card.Header>
        <Card.Meta>
          Local readiness checklist for indexer/download-client style integration.
        </Card.Meta>
      </Card.Content>
      <Card.Content>
        <div className="integration-section-header">
          <Header as="h4">
            <Icon name="clipboard check" />
            Compatibility Review
          </Header>
          <Popup
            content="Copy the local Servarr compatibility review. This does not call Lidarr, create download clients, pull wanted items, or trigger imports."
            position="top center"
            trigger={
              <Button
                aria-label="Copy Servarr compatibility review"
                onClick={copyCompatibilityReport}
                size="small"
              >
                <Icon name="copy" />
                Copy Review
              </Button>
              }
            />
          <Popup
            content="Run ready Servarr actions now. Currently this calls the configured Lidarr wanted-sync endpoint when wanted pull is ready; imports still require an explicit directory in the Lidarr panel."
            position="top center"
            trigger={
              <Button
                aria-label="Run ready Servarr actions"
                disabled={!compatibility.supportsWantedPull}
                loading={running}
                onClick={runReadyActions}
                primary
                size="small"
              >
                <Icon name="play" />
                Run Ready
              </Button>
            }
          />
        </div>
        <div className="integration-status-row">
          <Label color={summary.status === 'Ready' ? 'green' : 'orange'}>
            <Icon name={summary.status === 'Ready' ? 'check circle' : 'warning sign'} />
            {summary.status}
          </Label>
          <Label>
            {summary.ready}/{summary.total} checks ready
          </Label>
          <Label color={compatibility.supportsWantedPull ? 'green' : 'grey'}>
            Wanted Pull {compatibility.supportsWantedPull ? 'Ready' : 'Not Ready'}
          </Label>
          <Label color={compatibility.supportsCompletedImport ? 'green' : 'grey'}>
            Import {compatibility.supportsCompletedImport ? 'Ready' : 'Not Ready'}
          </Label>
        </div>
        <Table
          celled
          compact
        >
          <Table.Header>
            <Table.Row>
              <Table.HeaderCell>Check</Table.HeaderCell>
              <Table.HeaderCell>Status</Table.HeaderCell>
              <Table.HeaderCell>Why it matters</Table.HeaderCell>
            </Table.Row>
          </Table.Header>
          <Table.Body>
            {checks.map((check) => (
              <Table.Row key={check.id}>
                <Table.Cell>{check.title}</Table.Cell>
                <Table.Cell>
                  {boolLabel(check.ready, 'Ready', 'Needs Setup')}
                </Table.Cell>
                <Table.Cell>{check.description}</Table.Cell>
              </Table.Row>
            ))}
          </Table.Body>
        </Table>
        <Message
          info
          size="small"
        >
          This checklist is diagnostic only. It does not register indexers,
          create download clients, pull wanted items, or trigger imports.
        </Message>
        {compatibility.actions.length > 0 && (
          <Message
            size="small"
            warning
          >
            <Message.Header>Compatibility Actions</Message.Header>
            <ul>
              {compatibility.actions.map((action) => (
                <li key={action}>{action}</li>
              ))}
            </ul>
          </Message>
        )}
        {copyStatus && (
          <Message
            info
            size="small"
          >
            {copyStatus}
          </Message>
        )}
      </Card.Content>
    </Card>
  );
};
};

export default ServarrReadinessPanel;

