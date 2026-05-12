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

export const formatTransferState = (state) => {
  switch (state) {
    case 'Completed, Succeeded':
      return 'Complete';
    case 'Completed, Cancelled':
      return 'Cancelled';
    case 'Completed, TimedOut':
      return 'Timed out';
    case 'Completed, Errored':
      return 'Error';
    case 'Completed, Rejected':
      return 'Rejected';
    case 'Completed, Aborted':
      return 'Aborted';
    default:
      return state;
  }
};
