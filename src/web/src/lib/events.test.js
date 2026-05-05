import api from './api';
import { list } from './events';

vi.mock('./api', () => ({
  default: {
    get: vi.fn(),
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
});
