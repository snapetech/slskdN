import api from './api';
import { createRadioStreamUrl, getPartyDirectory } from './listeningParty';

vi.mock('./api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('listeningParty', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('requests a fresh local ticket for a selected host snapshot', async () => {
    api.post.mockResolvedValue({ data: { streamUrl: '/api/v0/mesh-streams/opaque' } });
    await expect(createRadioStreamUrl('party/a', 'radio:track')).resolves.toBe('/api/v0/mesh-streams/opaque');
    expect(api.post).toHaveBeenCalledWith('/listed-radio/party%2Fa/tickets', { contentId: 'radio:track' });
  });

  it('rejects a host-controlled external playback URL', async () => {
    api.post.mockResolvedValue({ data: { streamUrl: 'https://untrusted.example/audio' } });
    await expect(createRadioStreamUrl('party', 'radio:track')).rejects.toThrow('Invalid radio stream ticket');
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
