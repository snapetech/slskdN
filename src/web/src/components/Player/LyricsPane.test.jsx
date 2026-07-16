import LyricsPane from './LyricsPane';
import React from 'react';
import { act, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';

describe('LyricsPane', () => {
  beforeEach(() => {
    Element.prototype.scrollIntoView = vi.fn();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('uses media events without a redundant timer and catches up after visibility returns', async () => {
    window.fetch = vi.fn(() =>
      Promise.resolve({
        json: () =>
          Promise.resolve({
            syncedLyrics: '[00:01.00]First line\n[00:02.00]Second line',
          }),
        ok: true,
      }),
    );
    const intervalSpy = vi.spyOn(window, 'setInterval');
    const audio = document.createElement('audio');
    render(
      <LyricsPane
        audioElement={audio}
        current={{ artist: 'Example Artist', title: 'Example Song' }}
        visible
      />,
    );

    await screen.findByText('First line');
    audio.currentTime = 1.5;
    await act(async () => {
      audio.dispatchEvent(new Event('timeupdate'));
    });
    expect(screen.getByText('First line')).toHaveClass('player-lyrics-line-active');

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: true,
    });
    audio.currentTime = 2.5;
    await act(async () => {
      audio.dispatchEvent(new Event('timeupdate'));
    });
    expect(screen.getByText('First line')).toHaveClass('player-lyrics-line-active');

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
    });
    expect(screen.getByText('Second line')).toHaveClass('player-lyrics-line-active');
    expect(intervalSpy.mock.calls.some(([, delay]) => delay === 500)).toBe(false);
  });

  it('derives artist and title from local filenames when metadata is placeholder text', async () => {
    window.fetch = vi.fn(() =>
      Promise.resolve({
        json: () =>
          Promise.resolve({
            syncedLyrics: '[00:01.00]First line\n[00:02.00]Second line',
          }),
        ok: true,
      }),
    );

    render(
      <LyricsPane
        audioElement={document.createElement('audio')}
        current={{
          artist: 'slskdN',
          fileName: 'Example Artist - Example Song.ogg',
          title: 'Example Artist - Example Song.ogg',
        }}
        visible
      />,
    );

    await screen.findByText('First line');

    expect(window.fetch).toHaveBeenCalledWith(
      'https://lrclib.net/api/get?artist_name=Example+Artist&track_name=Example+Song',
      expect.any(Object),
    );
  });

  it('falls back to LRCLIB search when exact lyrics lookup misses', async () => {
    window.fetch = vi
      .fn()
      .mockResolvedValueOnce({
        json: () => Promise.resolve(null),
        ok: false,
      })
      .mockResolvedValueOnce({
        json: () =>
          Promise.resolve([
            {
              plainLyrics: 'Plain line one\nPlain line two',
            },
          ]),
        ok: true,
      });

    render(
      <LyricsPane
        audioElement={document.createElement('audio')}
        current={{
          artist: 'Example Artist',
          fileName: 'Example Song.ogg',
          title: 'Example Song',
        }}
        visible
      />,
    );

    await screen.findByText('Plain line one');

    await waitFor(() => {
      expect(window.fetch).toHaveBeenCalledWith(
        'https://lrclib.net/api/search?artist_name=Example+Artist&track_name=Example+Song',
        expect.any(Object),
      );
    });
  });

  it('does not call LRCLIB when artist metadata cannot be inferred', () => {
    window.fetch = vi.fn();

    render(
      <LyricsPane
        audioElement={document.createElement('audio')}
        current={{
          artist: 'slskdN',
          fileName: 'Sample2-public-domain-bansuri.ogg',
          title: 'Sample2-public-domain-bansuri.ogg',
        }}
        visible
      />,
    );

    expect(screen.getByText('Lyrics need artist and title metadata')).toBeInTheDocument();
    expect(window.fetch).not.toHaveBeenCalled();
  });
});
