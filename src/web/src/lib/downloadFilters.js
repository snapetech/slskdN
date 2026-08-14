const getOption = (source, ...keys) => {
  for (const key of keys) {
    if (source && Object.prototype.hasOwnProperty.call(source, key)) {
      return source[key];
    }
  }

  return undefined;
};

export const DOWNLOAD_FILTER_MAX_EXCLUSIONS = 100;
export const DOWNLOAD_FILTER_MAX_EXCLUSION_LENGTH = 256;

export const parseDownloadExclusions = (value = '') =>
  String(value ?? '')
    .split(/\r?\n|\r/)
    .map((line) => line.trim())
    .filter(Boolean);

export const formatDownloadExclusions = (exclusions = []) => {
  if (Array.isArray(exclusions)) {
    return exclusions.join('\n');
  }

  return String(exclusions ?? '');
};

export const getDownloadExclusions = (options = {}) => {
  const filters = getOption(options, 'filters', 'Filters') || {};
  const download = getOption(filters, 'download', 'Download') || {};
  const exclusions = getOption(download, 'exclude', 'Exclude');

  if (Array.isArray(exclusions)) {
    return exclusions.map((term) => String(term));
  }

  return exclusions === undefined || exclusions === null
    ? []
    : parseDownloadExclusions(exclusions);
};

export const getDownloadExclusionsValidationError = (value = '') => {
  const exclusions = parseDownloadExclusions(value);

  if (exclusions.length > DOWNLOAD_FILTER_MAX_EXCLUSIONS) {
    return `Download filters support at most ${DOWNLOAD_FILTER_MAX_EXCLUSIONS} terms.`;
  }

  const longTermIndex = exclusions.findIndex(
    (term) => term.length > DOWNLOAD_FILTER_MAX_EXCLUSION_LENGTH,
  );

  if (longTermIndex >= 0) {
    return `Download filter term ${longTermIndex + 1} is longer than ${DOWNLOAD_FILTER_MAX_EXCLUSION_LENGTH} characters.`;
  }

  return null;
};

export const setDownloadExclusionsInYaml = (document, value) => {
  const exclusions = Array.isArray(value)
    ? value.map((term) => String(term).trim()).filter(Boolean)
    : parseDownloadExclusions(value);

  document.setIn(['filters', 'download', 'exclude'], exclusions);
  return exclusions;
};
