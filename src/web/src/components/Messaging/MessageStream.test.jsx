import '@testing-library/jest-dom';
import MessageStream from './MessageStream';
import { __test__ } from './messagingAdapters';
import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

const { classifyBody } = __test__;

describe('classifyBody', () => {
  it('treats plain text as text', () => {
    expect(classifyBody('hi there')).toEqual({ body: 'hi there', kind: 'text' });
  });

  it('detects /me prefix and strips it', () => {
    expect(classifyBody('/me waves')).toEqual({ body: 'waves', kind: 'me' });
  });

  it('detects CTCP ACTION and strips wrappers', () => {
    const ctcp = `ACTION dances`;
    expect(classifyBody(ctcp)).toEqual({ body: 'dances', kind: 'me' });
  });

  it('parses listen-along JSON payload as a card', () => {
    const payload = {
      action: 'play',
      artist: 'Aphex Twin',
      kind: 'slskdn.listenAlong.v1',
      title: 'Xtal',
    };
    const result = classifyBody(JSON.stringify(payload));
    expect(result.kind).toBe('listenalong');
    expect(result.meta.artist).toBe('Aphex Twin');
  });

  it('ignores unrelated JSON', () => {
    const result = classifyBody('{"foo":"bar"}');
    expect(result.kind).toBe('text');
  });
});

const buildAdapter = (messages) => ({
  list: () => Promise.resolve({ messages }),
  pollIntervalMs: 1_000_000,
});

describe('MessageStream', () => {
  it('renders messages, collapses same-sender bursts, and shows listen-along card', async () => {
    const t = 1_700_000_000_000;
    const adapter = buildAdapter([
      { body: 'hey', id: 'a', isSelf: false, kind: 'text', sender: 'alice', ts: t },
      { body: 'still alice', id: 'b', isSelf: false, kind: 'text', sender: 'alice', ts: t + 5_000 },
      { body: 'hi alice', id: 'c', isSelf: true, kind: 'text', sender: 'me', ts: t + 10_000 },
      {
        body: 'whatever',
        id: 'd',
        isSelf: false,
        kind: 'listenalong',
        meta: { action: 'play', artist: 'Aphex Twin', title: 'Xtal' },
        sender: 'carol',
        ts: t + 15_000,
      },
    ]);

    render(<MessageStream adapter={adapter} />);

    await waitFor(() => {
      expect(screen.getByText('hey')).toBeInTheDocument();
    });

    expect(screen.getByText('still alice')).toBeInTheDocument();
    expect(screen.getByText('hi alice')).toBeInTheDocument();
    expect(screen.getByText('Xtal')).toBeInTheDocument();

    // alice nick should appear once (collapsed for the second message)
    const aliceNicks = screen.getAllByRole('button', { name: 'alice' });
    expect(aliceNicks).toHaveLength(1);

    // listen-along card uses its own marker
    expect(screen.getByText('— Aphex Twin')).toBeInTheDocument();
  });

  it('shows empty hint when there are no messages', async () => {
    const adapter = buildAdapter([]);
    render(<MessageStream adapter={adapter} emptyHint="nothing here yet" />);
    await waitFor(() => {
      expect(screen.getByText('nothing here yet')).toBeInTheDocument();
    });
  });
});
