// <copyright file="TransferTable.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import TransferTable from './TransferTable';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('TransferTable', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.stubGlobal(
      'ResizeObserver',
      class ResizeObserver {
        disconnect() {}

        observe() {}
      },
    );
  });

  afterEach(() => {
    localStorage.clear();
    vi.unstubAllGlobals();
  });

  const renderTable = () => render(
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

  it('labels the column chooser', () => {
    renderTable();

    expect(
      screen.getByRole('button', { name: 'Choose transfer table columns' }),
    ).toBeInTheDocument();
  });

  it('applies each resize movement from the drag-start width', () => {
    renderTable();

    const handle = screen.getByLabelText('Resize Name column');
    fireEvent.mouseDown(handle, { clientX: 100 });
    fireEvent.mouseMove(document, { clientX: 120 });
    fireEvent.mouseMove(document, { clientX: 121 });
    fireEvent.mouseUp(document);

    const saved = JSON.parse(localStorage.getItem('slskdn-transfer-columns-download'));
    expect(saved.widths.name).toBe(221);
  });

  it('resets persisted transfer column widths', () => {
    renderTable();

    const handle = screen.getByLabelText('Resize Name column');
    fireEvent.mouseDown(handle, { clientX: 100 });
    fireEvent.mouseMove(document, { clientX: 260 });
    fireEvent.mouseUp(document);

    fireEvent.click(screen.getByRole('button', { name: 'Reset transfer column widths' }));

    const saved = JSON.parse(localStorage.getItem('slskdn-transfer-columns-download'));
    expect(saved.widths.name).toBe(200);
  });
});
