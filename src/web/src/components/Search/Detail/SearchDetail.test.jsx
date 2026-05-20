import {
  getInitialResultFilters,
  mapUserNotesByUsername,
} from './SearchDetail';

describe('SearchDetail', () => {
  it('maps valid user notes and ignores malformed note entries', () => {
    expect(mapUserNotesByUsername([
      null,
      'bad',
      ['bad'],
      { note: 'missing username' },
      { note: 'trusted peer', username: 'alice' },
    ])).toEqual({
      alice: {
        note: 'trusted peer',
        username: 'alice',
      },
    });
  });

  it('returns an empty note map for malformed note payloads', () => {
    expect(mapUserNotesByUsername({ username: 'alice' })).toEqual({});
  });

  it('prefers URL filter over the saved default filter', () => {
    expect(getInitialResultFilters({
      getLocationSearch: () => '?filter=flac+OR+mp3',
      getStoredDefault: () => 'mp3',
    })).toBe('flac OR mp3');
  });

  it('uses saved default filter when URL does not provide one', () => {
    expect(getInitialResultFilters({
      getLocationSearch: () => '?other=value',
      getStoredDefault: () => 'lossless',
    })).toBe('lossless');
  });
});
