// <copyright file="FederationDiagnosticsPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import React, { useEffect, useState } from 'react';
import * as federationDiagnostics from '../../../lib/federationDiagnostics';
import {
  Button,
  Card,
  Header,
  Icon,
  Message,
  Segment,
  Table,
  Label,
} from 'semantic-ui-react';

const asArray = (value) => (Array.isArray(value) ? value : []);

const valueOrDash = (value) =>
  value === undefined || value === null || value === '' ? '-' : value;

const boolLabel = (value, trueText = 'Enabled', falseText = 'Disabled') => (
  <Label>
    <Label.Detail>
      <Icon color={value ? 'green' : 'grey'} name={value ? 'check' : 'close'} />
    </Label.Detail>
    {value ? trueText : falseText}
  </Label>
);

const FederationDiagnosticsPanel = () => {
  const [diagnostics, setDiagnostics] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    let mounted = true;

    const loadDiagnostics = async () => {
      try {
        setLoading(true);
        setError(null);
        const response = await federationDiagnostics.getDiagnostics();
        if (mounted) {
          setDiagnostics(response.data || {});
        }
      } catch (error_) {
        if (mounted) {
          setError(error_);
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    };

    loadDiagnostics();

    return () => {
      mounted = false;
    };
  }, []);

  const federation = diagnostics?.federation || {};
  const publishing = diagnostics?.publishing || {};
  const pods = diagnostics?.pods || {};
  const mesh = diagnostics?.mesh || {};
  const warnings = asArray(diagnostics?.warnings);

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="share alternate" />
          Federation and Pod Diagnostics
        </Card.Header>
        <Card.Meta>
          Read-only posture for ActivityPub, pod signing, and mesh-adjacent
          publishing.
        </Card.Meta>
      </Card.Content>
      <Card.Content>
        {error && (
          <Message
            negative
            size="small"
          >
            Federation diagnostics could not be loaded.
          </Message>
        )}
        <div className="integration-status-row">
          {boolLabel(federation.enabled, 'Federation On', 'Federation Off')}
          <Label>
            <Icon name="privacy" />
            Exposure: {valueOrDash(federation.exposure)}
          </Label>
          {boolLabel(publishing.enabled, 'Publishing On', 'Publishing Off')}
          {boolLabel(
            federation.verifySignatures,
            'HTTP Signatures On',
            'HTTP Signatures Off',
          )}
        </div>
        <Table
          compact
          definition
          size="small"
        >
          <Table.Body>
            <Table.Row>
              <Table.Cell>Domain configured</Table.Cell>
              <Table.Cell>{federation.domainConfigured ? 'Yes' : 'No'}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Base URL configured</Table.Cell>
              <Table.Cell>{federation.baseUrlConfigured ? 'Yes' : 'No'}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Publishable domains</Table.Cell>
              <Table.Cell>{publishing.publishableDomains?.join(', ') || '-'}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Publishing visibility</Table.Cell>
              <Table.Cell>{valueOrDash(publishing.defaultVisibility)}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Pod join signatures</Table.Cell>
              <Table.Cell>{valueOrDash(pods.joinSignatureMode)}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Pod message signatures</Table.Cell>
              <Table.Cell>{valueOrDash(pods.messageSignatureMode)}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Mesh self peer ID</Table.Cell>
              <Table.Cell>{mesh.selfPeerIdConfigured ? 'Configured' : 'Missing'}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Soulseek rendezvous</Table.Cell>
              <Table.Cell>{mesh.soulseekRendezvousEnabled ? 'Enabled' : 'Disabled'}</Table.Cell>
            </Table.Row>
          </Table.Body>
        </Table>
        {warnings.length > 0 && (
          <Message
            size="small"
            warning
          >
            <Message.Header>Federation posture warnings</Message.Header>
            <Message.List items={warnings} />
          </Message>
        )}
        {!loading && warnings.length === 0 && diagnostics && (
          <Message
            positive
            size="small"
          >
            No federation or pod signing posture warnings were reported.
          </Message>
        )}
      </Card.Content>
    </Card>
  );
};

export default FederationDiagnosticsPanel;
