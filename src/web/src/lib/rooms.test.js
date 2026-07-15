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

  it('posts room ticker messages as JSON strings', async () => {
    api.post.mockResolvedValue({ data: {} });

    await rooms.setTicker({ roomName: 'ambient', message: 'ticker text' });

    expect(api.post).toHaveBeenCalledWith(
      '/rooms/joined/ambient/ticker',
      '"ticker text"',
    );
  });

  it('posts room member names as JSON strings', async () => {
    api.post.mockResolvedValue({ data: {} });

    await rooms.addRoomMember({ roomName: 'ambient', username: 'alice' });

    expect(api.post).toHaveBeenCalledWith(
      '/rooms/joined/ambient/members',
      '"alice"',
    );
  });

  it('returns empty arrays for malformed room list payloads', async () => {
    api.get.mockResolvedValue({ data: { unexpected: true } });

    await expect(rooms.getAvailable()).resolves.toEqual([]);
    await expect(rooms.getJoined()).resolves.toEqual([]);
    await expect(rooms.getMessages({ roomName: 'ambient' })).resolves.toEqual([]);
    await expect(rooms.getUsers({ roomName: 'ambient' })).resolves.toEqual([]);
  });

  it('requests room-message deltas with an encoded cursor', async () => {
    api.get.mockResolvedValue({ data: [] });

    await rooms.getMessages({ roomName: 'ambient/lounge', since: 1_234 });

    expect(api.get).toHaveBeenCalledWith(
      '/rooms/joined/ambient%2Flounge/messages?since=1234',
    );
  });

  it('normalizes room activity timestamps and rejects malformed payloads', async () => {
    api.get.mockResolvedValueOnce({
      data: {
        ambient: 1_752_576_120_000,
        broken: 'not-a-timestamp',
        empty: 0,
      },
    });

    await expect(rooms.getActivity()).resolves.toEqual({
      ambient: 1_752_576_120_000,
    });
    expect(api.get).toHaveBeenCalledWith('/rooms/activity');

    api.get.mockResolvedValueOnce({ data: [] });
    await expect(rooms.getActivity()).resolves.toEqual({});
  });
});
