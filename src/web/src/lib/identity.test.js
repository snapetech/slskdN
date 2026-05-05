// <copyright file="identity.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as identity from './identity';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    put: vi.fn(),
  },
}));

describe('identity', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('encodes contact route segments', async () => {
    api.delete.mockResolvedValue({});
    api.get.mockResolvedValue({});
    api.put.mockResolvedValue({});

    await identity.getContact('contact/1');
    await identity.updateContact('contact/1', { alias: 'friend' });
    await identity.deleteContact('contact/1');

    expect(api.get).toHaveBeenCalledWith('/contacts/contact%2F1');
    expect(api.put).toHaveBeenCalledWith('/contacts/contact%2F1', { alias: 'friend' });
    expect(api.delete).toHaveBeenCalledWith('/contacts/contact%2F1');
  });
});
