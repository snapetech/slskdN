import * as identityAPI from '../../lib/identity';
import Contacts from './Contacts';
import QRCode from 'qrcode';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/identity', () => ({
  addContactFromInvite: vi.fn(),
  getContacts: vi.fn(),
  getNearby: vi.fn(),
  createInvite: vi.fn(),
}));

vi.mock('qrcode', () => ({
  default: {
    toDataURL: vi.fn(),
  },
}));

const renderContacts = () =>
  render(
    <MemoryRouter>
      <Contacts />
    </MemoryRouter>,
  );

describe('Contacts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    identityAPI.getContacts.mockResolvedValue({ data: [] });
    identityAPI.getNearby.mockResolvedValue({ data: [] });
    identityAPI.createInvite.mockResolvedValue({
      data: {
        friendCode: 'FRIEND-1234',
        inviteLink: 'slskdn://invite/test-invite',
      },
    });
    QRCode.toDataURL.mockResolvedValue('data:image/png;base64,inviteqr');
  });

  afterEach(() => {
    vi.restoreAllMocks();
    delete window.BarcodeDetector;
    delete window.createImageBitmap;
  });

  it('renders a QR code for newly created invites', async () => {
    renderContacts();

    fireEvent.click(await screen.findByText('Create Invite'));

    expect(await screen.findByTestId('contacts-invite-output')).toHaveValue(
      'slskdn://invite/test-invite',
    );
    expect(screen.getByTestId('contacts-invite-qr')).toHaveAttribute(
      'src',
      'data:image/png;base64,inviteqr',
    );
    expect(QRCode.toDataURL).toHaveBeenCalledWith(
      'slskdn://invite/test-invite',
      {
        errorCorrectionLevel: 'M',
        margin: 2,
        scale: 6,
      },
    );
  });

  it('ignores malformed contact and nearby list payloads', async () => {
    identityAPI.getContacts.mockResolvedValue({
      data: [null, { nickname: 'No peer id' }, { nickname: 'Alice', peerId: 'alice-peer' }],
    });
    identityAPI.getNearby.mockResolvedValue({ data: { peers: [] } });

    renderContacts();

    expect(await screen.findByText('Create Invite')).toBeInTheDocument();
    expect(screen.queryByText('contacts.map is not a function')).not.toBeInTheDocument();
    await waitFor(() => expect(identityAPI.getContacts).toHaveBeenCalled());
    expect(screen.queryByText('No peer id')).not.toBeInTheDocument();
  });

  it('shows a stable error when invite creation returns a malformed payload', async () => {
    identityAPI.createInvite.mockResolvedValue({ data: null });

    renderContacts();

    fireEvent.click(await screen.findByText('Create Invite'));

    expect(
      await screen.findByText(
        /Identity invite response did not include an invite link/,
      ),
    ).toBeInTheDocument();
    expect(QRCode.toDataURL).not.toHaveBeenCalled();
  });

  it('renders structured add-contact errors as text', async () => {
    identityAPI.addContactFromInvite.mockRejectedValue({
      response: {
        data: {
          detail: 'Invite expired',
          status: 400,
          title: 'Bad Request',
        },
      },
    });

    renderContacts();

    fireEvent.click(await screen.findByText('Add Friend'));
    fireEvent.change(screen.getByTestId('contacts-add-invite-input'), {
      target: { value: 'slskdn://invite/expired' },
    });
    fireEvent.change(screen.getByTestId('contacts-contact-nickname'), {
      target: { value: 'Alice' },
    });
    fireEvent.click(screen.getByTestId('contacts-add-invite-submit'));

    expect(await screen.findByText(/Invite expired/)).toBeInTheDocument();
  });

  it('fills the invite input from a scanned QR image', async () => {
    const close = vi.fn();
    const detect = vi.fn().mockResolvedValue([
      {
        rawValue: 'slskdn://invite/scanned',
      },
    ]);

    window.BarcodeDetector = vi.fn(function BarcodeDetector() {
      return { detect };
    });
    window.createImageBitmap = vi.fn().mockResolvedValue({ close });

    renderContacts();

    fireEvent.click(await screen.findByText('Add Friend'));
    fireEvent.change(screen.getByTestId('contacts-add-invite-qr-file'), {
      target: {
        files: [new File(['qr'], 'invite.png', { type: 'image/png' })],
      },
    });

    await waitFor(() => {
      expect(screen.getByTestId('contacts-add-invite-input')).toHaveValue(
        'slskdn://invite/scanned',
      );
    });

    expect(window.BarcodeDetector).toHaveBeenCalledWith({ formats: ['qr_code'] });
    expect(detect).toHaveBeenCalled();
    expect(close).toHaveBeenCalled();
  });
});
