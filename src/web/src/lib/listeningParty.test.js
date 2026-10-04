import api from './api';
import { authHeaders } from './session';
import { isPassthroughEnabled } from './token';
import { createRadioStreamUrl, getPartyDirectory, stopPartyStateOnPageHide } from './listeningParty';

vi.mock('./api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
  buildApiUrl: vi.fn((path) => `/api/v0${path}`),
}));
vi.mock('./session', () => ({ authHeaders: vi.fn(() => ({ Authorization: 'Bearer host-token', 'X-CSRF-TOKEN': 'csrf-token' })) }));
vi.mock('./token', () => ({ isPassthroughEnabled: vi.fn(() => false) }));

describe('listeningParty', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
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

  it('requests a source refresh only for explicit manual refresh', async () => {
    api.get.mockResolvedValue({ data: [] });
    await getPartyDirectory();
    expect(api.get).toHaveBeenLastCalledWith('/listening-party');
    await getPartyDirectory({ refresh: true });
    expect(api.get).toHaveBeenLastCalledWith('/listening-party?refresh=true');
  });

  it('returns party directory arrays from the API', async () => {
    api.get.mockResolvedValue({ data: [{ podId: 'pod-a' }] });

    await expect(getPartyDirectory()).resolves.toEqual([{ podId: 'pod-a' }]);
  });

  it('returns an empty party directory for malformed API payloads', async () => {
    api.get.mockResolvedValue({ data: { podId: 'not-a-list' } });

    await expect(getPartyDirectory()).resolves.toEqual([]);
  });

  it('sends an authenticated keepalive Stop for document close', async () => {
    const fetch = vi.fn().mockResolvedValue({ ok: true });
    vi.stubGlobal('fetch', fetch);
    const event = { action: 'stop', partyId: 'party', podId: 'pod/a' };

    await stopPartyStateOnPageHide('pod/a', 'music', event, 'session-id');

    expect(authHeaders).toHaveBeenCalledWith({ csrf: true });
    expect(isPassthroughEnabled).toHaveBeenCalledOnce();
    expect(fetch).toHaveBeenCalledExactlyOnceWith('/api/v0/listening-party/pod%2Fa/music', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer host-token',
        'X-CSRF-TOKEN': 'csrf-token',
        'Content-Type': 'application/json',
        'X-Listen-Along-Host-Session': 'session-id',
      },
      body: JSON.stringify(event),
      credentials: 'include',
      keepalive: true,
    });
  });

  it('does not send the passthrough sentinel as a bearer token', async () => {
    const fetch = vi.fn().mockResolvedValue({ ok: true });
    vi.stubGlobal('fetch', fetch);
    isPassthroughEnabled.mockReturnValue(true);

    await stopPartyStateOnPageHide('pod', 'music', { action: 'stop' }, 'session-id');

    expect(fetch.mock.calls[0][1].headers).not.toHaveProperty('Authorization');
    expect(authHeaders).not.toHaveBeenCalled();
  });
});
