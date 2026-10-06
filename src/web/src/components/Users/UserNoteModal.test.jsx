// <copyright file="UserNoteModal.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as userNotes from '../../lib/userNotes';
import React from 'react';
import UserNoteModal from './UserNoteModal';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/userNotes', () => ({
  getNote: vi.fn(),
  setNote: vi.fn(),
}));

describe('UserNoteModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    userNotes.setNote.mockResolvedValue({});
  });

  it('defaults malformed note responses instead of rendering invalid state', async () => {
    userNotes.getNote.mockResolvedValue({
      data: {
        color: 'not-a-semantic-color',
        isHighPriority: 'yes',
        note: { text: 'bad' },
      },
    });

    render(
      <UserNoteModal
        trigger={<button type="button">Open note</button>}
        username="alice"
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Open note' }));

    await waitFor(() =>
      expect(userNotes.getNote).toHaveBeenCalledWith({ username: 'alice' }),
    );

    expect(screen.getByPlaceholderText('Enter notes about this user...')).toHaveValue('');
    expect(screen.getByRole('checkbox')).not.toBeChecked();
  });

  it('saves normalized defaults after a null note response', async () => {
    userNotes.getNote.mockResolvedValue({ data: null });

    render(
      <UserNoteModal
        trigger={<button type="button">Open note</button>}
        username="bob"
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Open note' }));

    await waitFor(() =>
      expect(userNotes.getNote).toHaveBeenCalledWith({ username: 'bob' }),
    );

    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() =>
      expect(userNotes.setNote).toHaveBeenCalledWith({
        color: null,
        isHighPriority: false,
        note: '',
        username: 'bob',
      }),
    );
  });

  it('explains how to save or discard a user note', async () => {
    userNotes.getNote.mockResolvedValue({ data: null });

    render(
      <UserNoteModal
        trigger={<button type="button">Open note</button>}
        username="carol"
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Open note' }));
    await waitFor(() =>
      expect(userNotes.getNote).toHaveBeenCalledWith({ username: 'carol' }),
    );

    const cancelButton = screen.getByRole('button', { name: 'Cancel' });
    fireEvent.mouseEnter(cancelButton);
    expect(
      await screen.findByText(
        "Close without saving changes to this user's note.",
      ),
    ).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled(),
    );
    const saveButton = screen.getByRole('button', { name: 'Save' });
    fireEvent.mouseEnter(saveButton);
    expect(
      await screen.findByText(
        "Save this user's color rating, priority flag, and note.",
      ),
    ).toBeInTheDocument();
  });
});
