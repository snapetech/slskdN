import '@testing-library/jest-dom';
import UserCard from './UserCard';
import * as opinions from '../../lib/opinions';
import * as security from '../../lib/security';
import * as users from '../../lib/users';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { vi } from 'vitest';

vi.mock('../../lib/opinions', () => ({
  getOpinionSummary: vi.fn(),
}));

vi.mock('../../lib/security', () => ({
  getReputation: vi.fn(),
}));

vi.mock('../../lib/soulseekDiscovery', () => ({
  getUserInterests: vi.fn(),
}));

vi.mock('../../lib/users', () => ({
  getInfo: vi.fn(),
}));

describe('UserCard', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.clearAllMocks();
    users.getInfo.mockResolvedValue({
      data: {
        hasFreeUploadSlot: true,
        queueLength: 0,
        uploadSpeed: 2048,
      },
    });
    security.getReputation.mockResolvedValue({ score: 88 });
    opinions.getOpinionSummary.mockResolvedValue({
      data: {
        total: 1,
        weightedScore: 0.5,
      },
    });
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  it('defers and dedupes optional username metadata lookups', async () => {
    render(
      <>
        <UserCard username="alice">alice</UserCard>
        <UserCard username="alice">alice</UserCard>
        <UserCard username="alice">alice</UserCard>
      </>,
    );

    expect(screen.getAllByText('alice')).toHaveLength(3);
    expect(users.getInfo).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(300);

    await waitFor(() => expect(users.getInfo).toHaveBeenCalledTimes(1));
    expect(security.getReputation).toHaveBeenCalledTimes(1);
    expect(opinions.getOpinionSummary).toHaveBeenCalledTimes(1);
  });
});
