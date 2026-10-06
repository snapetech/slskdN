// <copyright file="SendMessageModal.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import SendMessageModal from './SendMessageModal';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

describe('SendMessageModal', () => {
  it('explains why Send is disabled and what the modal actions do', async () => {
    const initiateConversation = vi.fn().mockResolvedValue(undefined);

    render(
      <SendMessageModal
        initiateConversation={initiateConversation}
        trigger={<button type="button">New message</button>}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'New message' }));

    const cancelButton = await screen.findByRole('button', { name: 'Cancel' });
    fireEvent.mouseEnter(cancelButton);
    expect(
      await screen.findByText(
        'Close this dialog without sending the private message.',
      ),
    ).toBeInTheDocument();

    const sendButton = screen.getByRole('button', { name: 'Send' });
    expect(sendButton).toBeDisabled();
    fireEvent.mouseEnter(sendButton.parentElement);
    expect(
      await screen.findByText('Enter a username and message before sending.'),
    ).toBeInTheDocument();

    fireEvent.change(screen.getByPlaceholderText('Username'), {
      target: { value: 'alice' },
    });
    fireEvent.change(screen.getByPlaceholderText('Message'), {
      target: { value: 'Hello' },
    });

    const enabledSendButton = screen.getByRole('button', { name: 'Send' });
    expect(enabledSendButton).toBeEnabled();
    fireEvent.click(enabledSendButton);

    await waitFor(() => {
      expect(initiateConversation).toHaveBeenCalledWith('alice', 'Hello');
    });
  });
});
