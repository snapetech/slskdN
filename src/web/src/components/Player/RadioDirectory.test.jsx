// <copyright file="RadioDirectory.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import React from 'react';
import { act, cleanup, fireEvent, render, screen } from '@testing-library/react';
import { vi } from 'vitest';
import RadioDirectory from './RadioDirectory';
import * as listeningParty from '../../lib/listeningParty';

vi.mock('../../lib/listeningParty', () => ({ getPartyDirectory: vi.fn() }));

const party = { partyId: 'radio-a', title: 'Radio track', artist: 'Radio artist', contentId: 'sha256:radio', allowMeshStreaming: true, streamPath: '/radio/stream', transportUsername: 'host-overlay', streamTicket: 'capability' };

beforeEach(() => {
  vi.resetAllMocks();
  listeningParty.getPartyDirectory.mockResolvedValue([party, { ...party, partyId: 'metadata-a', title: 'Metadata track', allowMeshStreaming: false }]);
});
afterEach(cleanup);

it('plays only host-enabled streams through an explicit action', async () => {
  const onPlay = vi.fn();
  const onClose = vi.fn();
  render(<RadioDirectory onClose={onClose} onPlay={onPlay} />);
  const play = await screen.findByRole('button', { name: 'Play Radio track from listed radio' });
  expect(screen.getByRole('button', { name: 'Play Metadata track from listed radio' })).toBeDisabled();
  expect(onPlay).not.toHaveBeenCalled();
  fireEvent.click(play);
  expect(onPlay).toHaveBeenCalledWith(party);
  expect(onClose).toHaveBeenCalledOnce();
  expect(listeningParty.getPartyDirectory).toHaveBeenCalledOnce();
});

it('shows a directory failure and retries on manual refresh', async () => {
  listeningParty.getPartyDirectory.mockRejectedValueOnce(new Error('Unavailable')).mockResolvedValueOnce([]);
  render(<RadioDirectory onClose={vi.fn()} onPlay={vi.fn()} />);
  expect(await screen.findByRole('alert')).toHaveTextContent('Listed radio could not load');
  fireEvent.click(screen.getByRole('button', { name: 'Refresh listed radio' }));
  expect(await screen.findByText('No radio broadcasts are listed right now.')).toBeInTheDocument();
  expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  expect(listeningParty.getPartyDirectory).toHaveBeenCalledTimes(2);
  expect(listeningParty.getPartyDirectory).toHaveBeenNthCalledWith(1, { refresh: false });
  expect(listeningParty.getPartyDirectory).toHaveBeenNthCalledWith(2, { refresh: true });
});

it('ignores a directory response after closing and keeps refresh disabled while loading', async () => {
  let finish;
  listeningParty.getPartyDirectory.mockReturnValueOnce(new Promise((resolve) => { finish = resolve; }));
  const view = render(<RadioDirectory onClose={vi.fn()} onPlay={vi.fn()} />);
  expect(screen.getByRole('button', { name: 'Refresh listed radio' })).toBeDisabled();
  view.unmount();
  await act(async () => finish([party]));
  expect(screen.queryByText('Radio track')).not.toBeInTheDocument();
});
