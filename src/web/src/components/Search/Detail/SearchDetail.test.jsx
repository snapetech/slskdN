import { mapUserNotesByUsername } from './SearchDetail';

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
});
