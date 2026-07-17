// <copyright file="TransferTable.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import TransferTable from './TransferTable';
import React from 'react';
import { render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('TransferTable', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'ResizeObserver',
      class ResizeObserver {
        disconnect() {}

        observe() {}
      },
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('labels the column chooser', () => {
    render(
      <TransferTable
        direction="download"
        onCancel={vi.fn()}
        onCancelSelected={vi.fn()}
        onOpenRequest={vi.fn()}
        onRemove={vi.fn()}
        onRemoveSelected={vi.fn()}
        onRetry={vi.fn()}
        onRetrySelected={vi.fn()}
        onSelectAll={vi.fn()}
        onSelectionChange={vi.fn()}
        selectedFiles={[]}
        selectedKeys={[]}
        transfers={[]}
      />,
    );

    expect(
      screen.getByRole('button', { name: 'Choose transfer table columns' }),
    ).toBeInTheDocument();
  });
});
