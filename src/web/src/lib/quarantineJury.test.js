import api from './api';
import { getRequests } from './quarantineJury';

vi.mock('./api', () => ({
  default: {
    get: vi.fn(),
  },
}));

describe('quarantineJury', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns request arrays from the API', async () => {
    api.get.mockResolvedValue({ data: [{ id: 'request-1' }] });

    await expect(getRequests()).resolves.toEqual([{ id: 'request-1' }]);
  });

  it('returns an empty request list for malformed API payloads', async () => {
    api.get.mockResolvedValue({ data: { id: 'not-a-list' } });

    await expect(getRequests()).resolves.toEqual([]);
  });
});
