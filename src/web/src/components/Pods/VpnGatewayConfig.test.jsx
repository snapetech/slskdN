// <copyright file="VpnGatewayConfig.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as pods from '../../lib/pods';
import VpnGatewayConfig from './VpnGatewayConfig';
import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';
import { vi } from 'vitest';

vi.mock('../../lib/pods', () => ({ update: vi.fn() }));

const policy = {
  allowedDestinations: [
    { hostPattern: 'service.internal', port: 443, protocol: 'tcp' },
  ],
  allowPrivateRanges: true,
  allowPublicDestinations: false,
  dialTimeout: '00:00:30',
  enabled: true,
  gatewayPeerId: 'peer:test',
  idleTimeout: '01:00:00',
  maxBytesPerDayPerPeer: 1_073_741_824,
  maxConcurrentTunnelsPerPeer: 5,
  maxConcurrentTunnelsPod: 15,
  maxLifetime: '24:00:00',
  maxMembers: 3,
  maxNewTunnelsPerMinutePerPeer: 10,
  registeredServices: [
    {
      description: 'Internal web app',
      destinationHost: 'service.internal',
      destinationPort: 443,
      kind: 'WebInterface',
      name: 'Web app',
      protocol: 'tcp',
    },
  ],
};

const hoverForTooltip = async (button, tooltip) => {
  fireEvent.mouseEnter(button);
  expect(await screen.findByText(tooltip)).toBeInTheDocument();
};

describe('VPN Gateway Configuration tabs', () => {
  it('renders the basic settings content beneath the tab menu', () => {
    render(
      <VpnGatewayConfig
        podDetail={{ capabilities: ['PrivateServiceGateway'] }}
        podId="pod:test"
      />,
    );

    expect(screen.getByText('Enable VPN Gateway')).toBeInTheDocument();
  });

  it('explains each policy action before a click changes the draft or saves it', async () => {
    render(
      <VpnGatewayConfig
        podDetail={{
          capabilities: ['PrivateServiceGateway'],
          privateServicePolicy: policy,
        }}
        podId="pod:test"
      />,
    );

    await hoverForTooltip(
      screen.getByRole('button', { name: 'Save VPN Configuration' }),
      "Save this pod's enabled VPN gateway policy so its destination rules, registered services, and limits take effect.",
    );

    fireEvent.click(screen.getByText('Allowed Destinations'));
    const addDestination = screen.getByRole('button', {
      name: 'Add an allowed destination',
    });
    await hoverForTooltip(
      addDestination,
      "Add a host and port to this pod's allowed destination draft. Save the VPN configuration to apply the new route.",
    );
    await hoverForTooltip(
      screen.getByRole('button', {
        name: 'Remove allowed destination service.internal:443',
      }),
      'Remove service.internal:443 from the draft policy. Save the VPN configuration to stop allowing this route.',
    );
    expect(pods.update).not.toHaveBeenCalled();

    fireEvent.click(addDestination);
    await hoverForTooltip(
      screen.getByRole('button', { name: 'Add destination to draft policy' }),
      'Add this host, port, and protocol to the draft allowlist. Save the VPN configuration to apply it to the pod.',
    );
    await hoverForTooltip(
      screen.getByRole('button', { name: 'Cancel' }),
      'Close the dialog and return to the allowed destination list without adding this entry.',
    );
    expect(pods.update).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    fireEvent.click(screen.getByText('Registered Services'));
    const addService = screen.getByRole('button', {
      name: 'Add a registered service',
    });
    await hoverForTooltip(
      addService,
      "Register a named service and destination in this pod's policy draft so members can find the route. Save the VPN configuration to apply it.",
    );
    await hoverForTooltip(
      screen.getByRole('button', { name: 'Remove registered service Web app' }),
      'Remove Web app from the draft service list. Save the VPN configuration to stop advertising this route to pod members.',
    );

    fireEvent.click(addService);
    await hoverForTooltip(
      screen.getByRole('button', { name: 'Add service to draft policy' }),
      'Add this named service and destination to the draft policy. Save the VPN configuration to publish the route to pod members.',
    );
    await hoverForTooltip(
      screen.getByRole('button', { name: 'Cancel' }),
      'Close the dialog and return to registered services without adding this entry.',
    );
    expect(pods.update).not.toHaveBeenCalled();
  });
});
