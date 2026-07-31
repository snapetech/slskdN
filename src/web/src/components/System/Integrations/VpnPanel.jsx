// <copyright file="VpnPanel.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import React, { useState } from 'react';
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
  Table,
} from 'semantic-ui-react';

const getOption = (source, ...keys) => {
  for (const key of keys) {
    if (source && Object.prototype.hasOwnProperty.call(source, key)) {
      return source[key];
    }
  }
  return undefined;
};

const valueOrDash = (value) =>
  value === undefined || value === null || value === '' ? '-' : value;

const formatBytes = (value) => {
  if (!Number.isFinite(value)) return '-';
  if (value < 1024) return `${value} B`;
  const units = ['KiB', 'MiB', 'GiB', 'TiB'];
  let size = value / 1024;
  let unit = units[0];
  for (let index = 1; index < units.length && size >= 1024; index += 1) {
    size /= 1024;
    unit = units[index];
  }
  return `${size.toFixed(1)} ${unit}`;
};

const getIntegrationsOptions = (options = {}) =>
  getOption(options, 'integration', 'Integration', 'integrations', 'Integrations') || {};

const getVpnOptions = (options = {}) =>
  getOption(getIntegrationsOptions(options), 'vpn', 'Vpn', 'VPN') || {};

const getVpnState = (state = {}) => getOption(state, 'vpn', 'Vpn', 'VPN') || {};

const portForwards = (vpn = {}) =>
  (Array.isArray(vpn) ? vpn : (getOption(vpn, 'portForwards', 'PortForwards') || []));

const boolLabel = (value, trueText = 'Enabled', falseText = 'Disabled') => (
  <Label>
    <Label.Detail>
      <Icon color={value ? 'green' : 'grey'} name={value ? 'check' : 'close'} />
    </Label.Detail>
    {value ? trueText : falseText}
  </Label>
);

const VpnPanel = ({ options, state }) => {
  const vpnOptions = getVpnOptions(options);
  const vpnState = getVpnState(state);
  const gluetun = getOption(vpnOptions, 'gluetun', 'Gluetun') || {};
  const forwards = portForwards(vpnState);
  const relay = getOption(vpnState, 'relay', 'Relay');

  return (
    <Card fluid>
      <Card.Content>
        <Card.Header>
          <Icon name="shield alternate" />
          VPN
        </Card.Header>
        <Card.Meta>Daemon VPN readiness and configured provider settings.</Card.Meta>
      </Card.Content>
      <Card.Content>
        <div className="integration-status-row">
          {boolLabel(getOption(vpnOptions, 'enabled', 'Enabled'))}
          {boolLabel(
            getOption(vpnState, 'isReady', 'IsReady'),
            'Ready',
            'Not Ready',
          )}
          {boolLabel(
            getOption(vpnState, 'isConnected', 'IsConnected'),
            'Connected',
            'Disconnected',
          )}
          {boolLabel(
            getOption(vpnOptions, 'portForwarding', 'PortForwarding'),
            'Port Forwarding',
            'No Port Forwarding',
          )}
        </div>
        <Table
          basic="very"
          compact
          definition
        >
          <Table.Body>
            <Table.Row>
              <Table.Cell>Provider</Table.Cell>
              <Table.Cell>
                {getOption(vpnOptions, 'selfHostedRelay', 'SelfHostedRelay')
                  ? 'Self-hosted relay'
                  : (getOption(gluetun, 'url', 'Url') ? 'Gluetun' : '-')}
              </Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Control URL</Table.Cell>
              <Table.Cell>{valueOrDash(getOption(gluetun, 'url', 'Url'))}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Polling Interval</Table.Cell>
              <Table.Cell>
                {valueOrDash(getOption(vpnOptions, 'pollingInterval', 'PollingInterval'))}
                {' ms'}
              </Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Public IP</Table.Cell>
              <Table.Cell>
                {valueOrDash(getOption(vpnState, 'publicIPAddress', 'PublicIPAddress'))}
              </Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Location</Table.Cell>
              <Table.Cell>{valueOrDash(getOption(vpnState, 'location', 'Location'))}</Table.Cell>
            </Table.Row>
            <Table.Row>
              <Table.Cell>Forwarded Port</Table.Cell>
              <Table.Cell>
                {valueOrDash(getOption(vpnState, 'forwardedPort', 'ForwardedPort'))}
              </Table.Cell>
            </Table.Row>
          </Table.Body>
        </Table>
        {forwards.length > 0 && (
          <Table
            celled
            compact
          >
            <Table.Header>
              <Table.Row>
                <Table.HeaderCell>Slot</Table.HeaderCell>
                <Table.HeaderCell>Protocol</Table.HeaderCell>
                <Table.HeaderCell>Public</Table.HeaderCell>
                <Table.HeaderCell>Local</Table.HeaderCell>
              </Table.Row>
            </Table.Header>
            <Table.Body>
              {forwards.map((forward) => (
                <Table.Row key={`${forward.slot}-${forward.proto}-${forward.publicPort}`}>
                  <Table.Cell>{forward.slot}</Table.Cell>
                  <Table.Cell>{forward.proto}</Table.Cell>
                  <Table.Cell>
                    {valueOrDash(forward.publicIPAddress || forward.publicIp)}:
                    {forward.publicPort}
                  </Table.Cell>
                  <Table.Cell>
                    {forward.localPort || '-'}
                    {forward.targetPort && forward.targetPort !== forward.publicPort
                      ? ` -> ${forward.targetPort}`
                      : ''}
                  </Table.Cell>
                </Table.Row>
              ))}
            </Table.Body>
          </Table>
        )}
        {relay && (
          <Segment>
            <Header as="h4">
              <Icon name="exchange" />
              <Header.Content>Self-hosted relay</Header.Content>
            </Header>
            <div className="integration-status-row">
              {boolLabel(
                getOption(relay, 'connected', 'Connected'),
                'Tunnel Connected',
                'Tunnel Disconnected',
              )}
            </div>
            <Table basic="very" compact definition>
              <Table.Body>
                <Table.Row>
                  <Table.Cell>Transport</Table.Cell>
                  <Table.Cell>{valueOrDash(getOption(relay, 'transport', 'Transport'))}</Table.Cell>
                </Table.Row>
                <Table.Row>
                  <Table.Cell>Latency</Table.Cell>
                  <Table.Cell>
                    {valueOrDash(getOption(relay, 'latencyMs', 'LatencyMs'))} ms
                  </Table.Cell>
                </Table.Row>
                <Table.Row>
                  <Table.Cell>Traffic</Table.Cell>
                  <Table.Cell>
                    {formatBytes(getOption(relay, 'rxBytes', 'RxBytes'))} received /{' '}
                    {formatBytes(getOption(relay, 'txBytes', 'TxBytes'))} sent
                  </Table.Cell>
                </Table.Row>
                <Table.Row>
                  <Table.Cell>Connections</Table.Cell>
                  <Table.Cell>
                    {valueOrDash(getOption(relay, 'activeConnections', 'ActiveConnections'))} /{' '}
                    {valueOrDash(getOption(relay, 'connectionLimit', 'ConnectionLimit'))}
                  </Table.Cell>
                </Table.Row>
                <Table.Row>
                  <Table.Cell>Bandwidth Limit</Table.Cell>
                  <Table.Cell>
                    {valueOrDash(getOption(relay, 'bandwidthLimitMbit', 'BandwidthLimitMbit'))} Mbit/s
                  </Table.Cell>
                </Table.Row>
                <Table.Row>
                  <Table.Cell>
                    {getOption(relay, 'transport', 'Transport') === 'tailscale'
                      ? 'Latest Peer Activity'
                      : 'Latest Handshake'}
                  </Table.Cell>
                  <Table.Cell>
                    {valueOrDash(getOption(relay, 'latestHandshakeAt', 'LatestHandshakeAt'))}
                  </Table.Cell>
                </Table.Row>
                {getOption(relay, 'path', 'Path') && (
                  <Table.Row>
                    <Table.Cell>Peer Path</Table.Cell>
                    <Table.Cell>{getOption(relay, 'path', 'Path')}</Table.Cell>
                  </Table.Row>
                )}
              </Table.Body>
            </Table>
          </Segment>
        )}
      </Card.Content>
    </Card>
  );
};


export default VpnPanel;
