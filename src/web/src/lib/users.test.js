// <copyright file="users.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as users from './users';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
  },
}));

describe('users', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('requests encoded cached groups in one batch', async () => {
    api.get.mockResolvedValue({
      data: {
        'alice/example': 'privileged',
        bob: 'default',
      },
    });

    const groups = await users.getGroups({
      usernames: ['alice/example', 'bob'],
    });

    expect(api.get).toHaveBeenCalledWith(
      '/users/groups?usernames=alice%2Fexample&usernames=bob',
    );
    expect(groups).toEqual({
      'alice/example': 'privileged',
      bob: 'default',
    });
  });

  it('normalizes malformed batch responses to an empty map', async () => {
    api.get.mockResolvedValue({ data: [] });

    expect(await users.getGroups({ usernames: ['alice'] })).toEqual({});
  });
});
