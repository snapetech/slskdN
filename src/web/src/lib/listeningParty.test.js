import api from './api';
import { getPartyDirectory } from './listeningParty';

vi.mock('./api', () => ({
  default: {
    get: vi.fn(),
  },
}));

describe('listeningParty', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns party directory arrays from the API', async () => {
    api.get.mockResolvedValue({ data: [{ podId: 'pod-a' }] });

    await expect(getPartyDirectory()).resolves.toEqual([{ podId: 'pod-a' }]);
  });

  it('returns an empty party directory for malformed API payloads', async () => {
    api.get.mockResolvedValue({ data: { podId: 'not-a-list' } });

    await expect(getPartyDirectory()).resolves.toEqual([]);
  });
});
