import '@testing-library/jest-dom';
import Events from './index';
import { list } from '../../../lib/events';
import React from 'react';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../../lib/events', () => ({
  list: vi.fn(),
}));

describe('Events', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders malformed event data without crashing the table', async () => {
    list.mockResolvedValue({
      events: [
        {
          data: '{not-json',
          id: 'event-1',
          timestamp: '2026-05-05T18:30:00Z',
          type: 'MalformedPayload',
        },
      ],
      totalCount: 1,
    });

    render(<Events />);

    expect(await screen.findByText('{not-json')).toBeInTheDocument();
    expect(screen.getByText('MalformedPayload')).toBeInTheDocument();
  });

  it('pretty-prints valid event JSON data', async () => {
    list.mockResolvedValue({
      events: [
        {
          data: JSON.stringify({ message: 'ok' }),
          id: 'event-2',
          timestamp: '2026-05-05T18:31:00Z',
          type: 'ValidPayload',
        },
      ],
      totalCount: 1,
    });

    render(<Events />);

    expect(await screen.findByText(/"message": "ok"/)).toBeInTheDocument();
  });

  it('renders an empty table for malformed event list payloads', async () => {
    list.mockResolvedValue({
      events: [],
      totalCount: 1,
    });

    render(<Events />);

    expect(await screen.findByText('No events')).toBeInTheDocument();
  });
});
