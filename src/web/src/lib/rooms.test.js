// <copyright file="rooms.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as rooms from './rooms';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('rooms api', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('posts room names as JSON strings so ASP.NET string body binding works', async () => {
    api.post.mockResolvedValue({ data: {} });

    await rooms.join({ roomName: 'ambient' });

    expect(api.post).toHaveBeenCalledWith('/rooms/joined', '"ambient"');
  });

  it('posts room messages as JSON strings', async () => {
    api.post.mockResolvedValue({ data: {} });

    await rooms.sendMessage({ roomName: 'ambient', message: 'hello' });

    expect(api.post).toHaveBeenCalledWith(
      '/rooms/joined/ambient/messages',
      '"hello"',
    );
  });

  it('returns empty arrays for malformed room list payloads', async () => {
    api.get.mockResolvedValue({ data: { unexpected: true } });

    await expect(rooms.getAvailable()).resolves.toEqual([]);
    await expect(rooms.getJoined()).resolves.toEqual([]);
    await expect(rooms.getMessages({ roomName: 'ambient' })).resolves.toEqual([]);
    await expect(rooms.getUsers({ roomName: 'ambient' })).resolves.toEqual([]);
  });
});
