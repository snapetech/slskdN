import api from './api';
import {
  blockUser,
  getBlockedUsers,
  setBlockedUsers,
  unblockUser,
} from './searches';

const SERVER_MIGRATION_KEY = 'slskdn_blocked_users_server_migrated';

const normalizeServerBlocks = (payload) => {
  const records = Array.isArray(payload) ? payload : [];
  const names = records.map((record) =>
    typeof record === 'string' ? record : record?.username,
  );
  return names.filter((username) => typeof username === 'string');
};

const mergeUsernames = (...lists) => {
  const merged = [];
  const seen = new Set();
  lists.flat().forEach((username) => {
    const normalized = typeof username === 'string' ? username.trim() : '';
    const key = normalized.toLocaleLowerCase();
    if (normalized && !seen.has(key)) {
      seen.add(key);
      merged.push(normalized);
    }
  });
  return merged;
};

export const getBlockedUsersFromServer = async () => {
  const response = await api.get('/users/blocks');
  return normalizeServerBlocks(response.data);
};

export const syncBlockedUsers = async () => {
  const localUsers = getBlockedUsers();
  let serverUsers = await getBlockedUsersFromServer();
  const serverKeys = new Set(serverUsers.map((username) => username.toLocaleLowerCase()));
  const localOnly = localUsers.filter(
    (username) => !serverKeys.has(username.toLocaleLowerCase()),
  );

  // Migrate browser-only blocks and merge them with the durable list. A
  // successful merge is enough to make every browser share the same policy.
  await Promise.all(
    localOnly.map((username) =>
      api.put(`/users/blocks/${encodeURIComponent(username)}`),
    ),
  );
  serverUsers = mergeUsernames(serverUsers, localOnly);
  setBlockedUsers(serverUsers);

  if (typeof window !== 'undefined') {
    window.localStorage.setItem(SERVER_MIGRATION_KEY, 'true');
  }

  return serverUsers;
};

export const blockUserOnServer = async (username) => {
  await api.put(`/users/blocks/${encodeURIComponent(username.trim())}`);
  return blockUser(username);
};

export const unblockUserOnServer = async (username) => {
  await api.delete(`/users/blocks/${encodeURIComponent(username.trim())}`);
  return unblockUser(username);
};
