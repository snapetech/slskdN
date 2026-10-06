// <copyright file="FileList.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import '@testing-library/jest-dom';
import FileList from './FileList';
import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

describe('FileList action guidance', () => {
  it('explains folder collapse and close actions without invoking them on hover', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <FileList
        directoryName="Shared Music"
        files={[]}
        locked={false}
        onClose={onClose}
        onSelectionChange={vi.fn()}
      />,
    );

    await user.hover(screen.getByRole('button', { name: 'Collapse Shared Music' }));
    expect(await screen.findByText(/Hide the files inside Shared Music/))
      .toBeInTheDocument();
    const close = screen.getByRole('button', { name: 'Close Shared Music file list' });
    await user.hover(close);
    expect(await screen.findByText(/Close the Shared Music file list/))
      .toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });

  it('keeps an explanation available when the peer locks the directory', async () => {
    const user = userEvent.setup();
    render(
      <FileList
        directoryName="Private Files"
        files={[]}
        locked
        onSelectionChange={vi.fn()}
      />,
    );

    const locked = screen.getByRole('button', { name: 'Private Files is locked' });
    expect(locked).toBeDisabled();
    await user.hover(locked.parentElement);
    expect(await screen.findByText(/locked by the peer and cannot be expanded/))
      .toBeInTheDocument();
  });
});
