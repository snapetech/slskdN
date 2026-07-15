// <copyright file="chat.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as chat from './chat';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
  },
}));

describe('chat api', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns only an explicit true activity response', async () => {
    api.get.mockResolvedValueOnce({ data: true });

    await expect(chat.hasUnAcknowledgedMessages()).resolves.toBe(true);
    expect(api.get).toHaveBeenCalledWith('/conversations/activity/unacknowledged');

    api.get.mockResolvedValueOnce({ data: { unexpected: true } });
    await expect(chat.hasUnAcknowledgedMessages()).resolves.toBe(false);
  });

  it('adds an incremental timestamp cursor only when supplied', async () => {
    api.get.mockResolvedValue({ data: { messages: [] } });

    await chat.get({ username: 'user name' });
    await chat.get({ since: 1_234, username: 'user name' });

    expect(api.get).toHaveBeenNthCalledWith(1, '/conversations/user%20name');
    expect(api.get).toHaveBeenNthCalledWith(
      2,
      '/conversations/user%20name?since=1234',
    );
  });
});
