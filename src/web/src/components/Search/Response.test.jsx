// <copyright file="Response.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { buildSearchItemActionPath } from './Response';
import { buildPeerStreamUrl } from '../../lib/streaming';

describe('Search Response action routes', () => {
  it('encodes search and item identifiers per route segment', () => {
    expect(
      buildSearchItemActionPath('search/with?intent', '0:1/2', 'download'),
    ).toBe('/searches/search%2Fwith%3Fintent/items/0%3A1%2F2/download');
  });

  it('encodes the selected destination for bridged downloads', () => {
    expect(
      buildSearchItemActionPath(
        'search-id',
        '0:1',
        'download',
        '/downloads/Music & Audio',
      ),
    ).toBe(
      '/searches/search-id/items/0%3A1/download?destination=%2Fdownloads%2FMusic%20%26%20Audio',
    );
  });
});

describe('Peer stream URLs', () => {
  it('roots relative peer stream URLs at the configured app base', () => {
    expect(buildPeerStreamUrl('/api/v0/peer-streams/ticket-1')).toContain(
      '/api/v0/peer-streams/ticket-1',
    );
  });
});
