// <copyright file="index.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import Files from './index';
import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';

vi.mock('./Explorer', () => ({
  default: ({ active, remoteFileManagement, root }) => (
    <div
      data-active={active}
      data-remote-file-management={remoteFileManagement}
      data-testid={`explorer-${root}`}
    />
  ),
}));

describe('System Files tabs', () => {
  it('keeps both explorers mounted and activates the selected root', () => {
    render(<Files options={{ remoteFileManagement: true }} />);

    expect(screen.getByTestId('explorer-downloads')).toHaveAttribute(
      'data-active',
      'true',
    );
    expect(screen.getByTestId('explorer-incomplete')).toHaveAttribute(
      'data-active',
      'false',
    );
    expect(screen.getByTestId('explorer-downloads')).toHaveAttribute(
      'data-remote-file-management',
      'true',
    );

    fireEvent.click(screen.getByText('Incomplete'));

    expect(screen.getByTestId('explorer-downloads')).toHaveAttribute(
      'data-active',
      'false',
    );
    expect(screen.getByTestId('explorer-incomplete')).toHaveAttribute(
      'data-active',
      'true',
    );
  });
});
