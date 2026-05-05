import api from './api';
import {
  disconnectSpotify,
  getSpotifyStatus,
  startSpotifyAuthorization,
} from './spotifyIntegration';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('spotifyIntegration', () => {
  const originalLocation = window.location;

  beforeEach(() => {
    vi.clearAllMocks();
    delete window.location;
    window.location = {
      ...originalLocation,
      assign: vi.fn(),
    };
  });

  afterAll(() => {
    window.location = originalLocation;
  });

  it('loads Spotify connection status', async () => {
    api.get.mockResolvedValue({ data: { connected: true } });

    await expect(getSpotifyStatus()).resolves.toEqual({ connected: true });

    expect(api.get).toHaveBeenCalledWith('/integrations/spotify/status');
  });

  it('opens camelCase authorization URLs returned by the API', async () => {
    api.post.mockResolvedValue({
      data: { authorizationUrl: 'https://accounts.spotify.com/authorize?state=one' },
    });

    const result = await startSpotifyAuthorization();

    expect(result.authorizationUrl).toContain('spotify.com');
    expect(window.location.assign).toHaveBeenCalledWith(
      'https://accounts.spotify.com/authorize?state=one',
    );
  });

  it('opens snake_case authorization URLs returned by compatibility APIs', async () => {
    api.post.mockResolvedValue({
      data: { authorization_url: 'https://accounts.spotify.com/authorize?state=two' },
    });

    await startSpotifyAuthorization();

    expect(window.location.assign).toHaveBeenCalledWith(
      'https://accounts.spotify.com/authorize?state=two',
    );
  });

  it('disconnects Spotify account state', async () => {
    api.delete.mockResolvedValue({});

    await disconnectSpotify();

    expect(api.delete).toHaveBeenCalledWith('/integrations/spotify');
  });
});
