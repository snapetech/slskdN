import api from './api';
import { list, raiseEvent } from './events';

vi.mock('./api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('events', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns event arrays with total count headers', async () => {
    api.get.mockResolvedValue({
      data: [{ id: 'event-1' }],
      headers: { 'x-total-count': '1' },
    });

    await expect(list({ limit: 10, offset: 0 })).resolves.toEqual({
      events: [{ id: 'event-1' }],
      totalCount: '1',
    });
  });

  it('returns an empty event list for malformed payloads', async () => {
    api.get.mockResolvedValue({
      data: { id: 'not-a-list' },
      headers: { 'x-total-count': '1' },
    });

    await expect(list({ limit: 10, offset: 0 })).resolves.toEqual({
      events: [],
      totalCount: '1',
    });
  });

  it('raises typed events with a JSON string disambiguator', async () => {
    api.post.mockResolvedValue({ data: { event: 'noop' } });

    await raiseEvent({ type: 'noop', disambiguator: 'abc-123' });

    expect(api.post).toHaveBeenCalledWith(
      '/events/noop',
      '"abc-123"',
    );
  });
});
