import api from './api';
import {
  applyHumanChallengeAutoResponse,
  canApplyHumanChallengeAutoResponse,
  getHumanChallengeAutoResponseEnabled,
  humanChallengeAutoResponseStorageKey,
  readStoredHumanChallengeAutoResponse,
  writeStoredHumanChallengeAutoResponse,
} from './humanChallengeAutoResponse';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    patch: vi.fn(),
  },
}));

describe('humanChallengeAutoResponse', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.clearAllMocks();
  });

  it('persists the shared browser toggle', () => {
    writeStoredHumanChallengeAutoResponse(true);

    expect(window.localStorage.getItem(humanChallengeAutoResponseStorageKey)).toBe('true');
    expect(readStoredHumanChallengeAutoResponse()).toBe(true);
  });

  it('reads current options shape from the daemon', () => {
    expect(
      getHumanChallengeAutoResponseEnabled({
        soulseek: { privateMessageAutoResponse: { enabled: true } },
      }),
    ).toBe(true);
  });

  it('requires remote configuration for daemon-side apply', () => {
    expect(canApplyHumanChallengeAutoResponse({ remoteConfiguration: true })).toBe(true);
    expect(canApplyHumanChallengeAutoResponse({ remoteConfiguration: false })).toBe(false);
  });

  it('applies the runtime overlay', async () => {
    api.patch.mockResolvedValue({ data: {} });

    await applyHumanChallengeAutoResponse(true);

    expect(api.patch).toHaveBeenCalledWith('/options', {
      soulseek: {
        privateMessageAutoResponse: {
          enabled: true,
        },
      },
    });
  });
});
