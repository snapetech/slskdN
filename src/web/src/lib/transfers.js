import api from './api';

export const getAll = async ({ direction, includeCompleted = true }) => {
  const params = includeCompleted ? '' : '?includeCompleted=false';
  const response = (
    await api.get(`/transfers/${encodeURIComponent(direction)}s${params}`)
  ).data;

  if (!Array.isArray(response)) {
    console.warn('got non-array response from transfers API', response);
    return [];
  }

  return response;
};

export const getSpeeds = async () => {
  const response = await api.get('/transfers/speeds');
  return response.data;
};

export const getAcceleratedMode = async () => {
  const response = await api.get('/transfers/downloads/accelerated');
  return response.data;
};

export const setAcceleratedMode = async ({ enabled }) => {
  const response = await api.put('/transfers/downloads/accelerated', {
    enabled,
  });
  return response.data;
};

export const download = ({ username, files = [], destination }) => {
  const parameters = destination
    ? `?destination=${encodeURIComponent(destination)}`
    : '';
  return api.post(
    `/transfers/downloads/${encodeURIComponent(username)}${parameters}`,
    files,
  );
};

export const cancel = ({
  direction,
  username,
  id,
  remove = false,
  deleteFile = false,
}) => {
  return api.delete(
    `/transfers/${direction}s/${encodeURIComponent(username)}/${encodeURIComponent(id)}?remove=${remove}&deleteFile=${deleteFile}`,
  );
};

export const clearCompleted = ({ direction }) => {
  return api.delete(`/transfers/${direction}s/all/completed`);
};

// 'Requested'
// 'Queued, Remotely'
// 'Queued, Locally'
// 'Initializing'
// 'InProgress'
// 'Completed, Succeeded'
// 'Completed, Cancelled'
// 'Completed, TimedOut'
// 'Completed, Errored'
// 'Completed, Rejected'
// 'Completed, Aborted'

export const getPlaceInQueue = ({ username, id }) => {
  return api.get(
    `/transfers/downloads/${encodeURIComponent(username)}/${encodeURIComponent(id)}/position`,
  );
};

export const isStateSucceeded = (state) => state === 'Completed, Succeeded';

export const isStateTerminal = (state = '') => state.includes('Completed');

export const isStateRetryable = (state) =>
  isStateTerminal(state) && !isStateSucceeded(state);

export const isStateCancellable = (state) =>
  [
    'InProgress',
    'Requested',
    'Queued',
    'Queued, Remotely',
    'Queued, Locally',
    'Initializing',
  ].find((s) => s === state);

export const isStateRemovable = (state) => isStateTerminal(state);

const isRemoteUnavailableTransferError = (message = '') =>
  [
    'Remote connection closed',
    'Connection reset by peer',
    'Failed to establish a direct or indirect message connection',
    'Failed to establish a direct or indirect transfer connection',
    'Download reported as failed by remote client',
    'Transfer rejected:',
  ].some((token) => message.includes(token));

/**
 * Extracts a concise, user-facing reason from a transfer exception string.
 * Returns the original exception if no common pattern is identified.
 */
export const getFailureReason = (exception = '') => {
  if (!exception) return '';

  // TransferRejectedException: Transfer rejected: File not shared.
  const rejectedMatch = exception.match(/Transfer\s+rejected:\s*(.+?)\.?$/im);
  if (rejectedMatch) return rejectedMatch[1].trim();

  // TransferRejectedException: Transfer rejected: Enqueue failed due to internal error
  const enqueueFailedMatch = exception.match(/Enqueue\s+failed\s+due\s+to\s+(.+?)(?:;|\.|$)/im);
  if (enqueueFailedMatch) return enqueueFailedMatch[1].trim();

  // TransferSizeMismatchException: Transfer aborted: the remote size of X does not match expected size Y
  const sizeMismatchMatch = exception.match(/the\s+remote\s+size\s+of\s+\d+\s+does\s+not\s+match\s+expected\s+size\s+\d+/im);
  if (sizeMismatchMatch) return 'Size mismatch';

  // UserOfflineException: User X appears to be offline
  const offlineMatch = exception.match(/appears\s+to\s+be\s+offline/i);
  if (offlineMatch) return 'User offline';

  // Timeout
  const timeoutMatch = exception.match(/timed?\s*out/i);
  if (timeoutMatch) return 'Timed out';

  // Remote connection closed / Connection reset
  const connectionMatch = exception.match(/(Remote\s+connection\s+closed|Connection\s+reset\s+by\s+peer)/i);
  if (connectionMatch) return 'Connection lost';

  // Truncate verbose exception type prefixes
  return exception
    .replace(/^(Soulseek\.)?\w+Exception:\s*/i, '')
    .replace(/; remoteReason=.*$/, '')
    .trim();
};

export const formatTransferState = (state, exception = '') => {
  switch (state) {
    case 'Completed, Succeeded':
      return 'Complete';
    case 'Completed, Cancelled':
      return 'Cancelled';
    case 'Completed, TimedOut':
      return 'Timed out';
    case 'Completed, Errored':
      return isRemoteUnavailableTransferError(exception)
        ? 'Peer unavailable'
        : 'Error';
    case 'Completed, Rejected': {
      const reason = getFailureReason(exception);
      return reason ? `Rejected (${reason})` : 'Rejected';
    }
    case 'Completed, Aborted':
      return 'Aborted';
    default:
      return state;
  }
};
