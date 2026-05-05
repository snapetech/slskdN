// <copyright file="Response.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { buildSearchItemActionPath } from './Response';

describe('Search Response action routes', () => {
  it('encodes search and item identifiers per route segment', () => {
    expect(
      buildSearchItemActionPath('search/with?intent', '0:1/2', 'download'),
    ).toBe('/searches/search%2Fwith%3Fintent/items/0%3A1%2F2/download');
  });
});
