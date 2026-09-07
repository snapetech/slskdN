import api from './api';
import {
  blockUserOnServer,
  getBlockedUsersFromServer,
  syncBlockedUsers,
  unblockUserOnServer,
} from './userBlocks';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    put: vi.fn(),
  },
}));

describe('user blocks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('merges existing browser-only blocks into the durable server list', async () => {
    localStorage.setItem('slskdn_blocked_users', JSON.stringify(['LocalPeer']));
    api.get.mockResolvedValue({ data: [{ username: 'ServerPeer' }] });
    api.put.mockResolvedValue({});

    await expect(syncBlockedUsers()).resolves.toEqual(['ServerPeer', 'LocalPeer']);

    expect(api.get).toHaveBeenCalledWith('/users/blocks');
    expect(api.put).toHaveBeenCalledWith('/users/blocks/LocalPeer');
    expect(JSON.parse(localStorage.getItem('slskdn_blocked_users'))).toEqual([
      'ServerPeer',
      'LocalPeer',
    ]);
  });

  it('uses encoded API routes and updates the local compatibility cache', async () => {
    api.put.mockResolvedValue({});
    api.delete.mockResolvedValue({});

    await blockUserOnServer('peer/name');
    await unblockUserOnServer('peer/name');

    expect(api.put).toHaveBeenCalledWith('/users/blocks/peer%2Fname');
    expect(api.delete).toHaveBeenCalledWith('/users/blocks/peer%2Fname');
    expect(JSON.parse(localStorage.getItem('slskdn_blocked_users'))).toEqual([]);
  });

  it('normalizes the server payload to usernames', async () => {
    api.get.mockResolvedValue({
      data: [{ username: 'peer' }, 'another-peer', { note: 'not a block' }],
    });

    await expect(getBlockedUsersFromServer()).resolves.toEqual([
      'peer',
      'another-peer',
    ]);
  });
});
