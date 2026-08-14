import {
  formatDownloadExclusions,
  getDownloadExclusions,
  getDownloadExclusionsValidationError,
  parseDownloadExclusions,
  setDownloadExclusionsInYaml,
} from './downloadFilters';
import { describe, expect, it } from 'vitest';
import YAML from 'yaml';

describe('downloadFilters', () => {
  it('reads the configured filter across API casing styles', () => {
    expect(
      getDownloadExclusions({
        Filters: {
          Download: {
            Exclude: ['acapella', 'instrumental'],
          },
        },
      }),
    ).toEqual(['acapella', 'instrumental']);
  });

  it('parses, formats, and validates one term per line', () => {
    expect(parseDownloadExclusions(' acapella\r\n\n instrumental ')).toEqual([
      'acapella',
      'instrumental',
    ]);
    expect(formatDownloadExclusions(['acapella', 'instrumental'])).toBe(
      'acapella\ninstrumental',
    );
    expect(getDownloadExclusionsValidationError('acapella\ninstrumental')).toBeNull();
    expect(getDownloadExclusionsValidationError('x'.repeat(257))).toContain(
      '256 characters',
    );
  });

  it('updates only the download exclusion YAML path', () => {
    const document = YAML.parseDocument(
      'filters:\n  search:\n    request: [lossless]\n',
    );

    setDownloadExclusionsInYaml(document, ' acapella\n instrumental ');

    expect(YAML.parse(document.toString())).toEqual({
      filters: {
        download: {
          exclude: ['acapella', 'instrumental'],
        },
        search: {
          request: ['lossless'],
        },
      },
    });
  });
});
