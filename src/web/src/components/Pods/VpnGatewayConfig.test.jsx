// <copyright file="VpnGatewayConfig.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import VpnGatewayConfig from './VpnGatewayConfig';
import { render, screen } from '@testing-library/react';
import React from 'react';

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
});
