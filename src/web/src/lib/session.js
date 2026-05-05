import { tokenPassthroughValue } from '../config';
import api, { getCsrfTokenFromCookieString } from './api';
import { clearToken, getToken, setToken } from './token';

export const getSecurityEnabled = async () => {
  return (await api.get('/session/enabled')).data;
};

export const enablePassthrough = () => {
  console.debug(
    'enabling token passthrough.  api calls will not be authenticated',
  );
  setToken(sessionStorage, tokenPassthroughValue);
};

export const isLoggedIn = () => {
  const token = getToken();
  return (
    token !== undefined && token !== null && token !== tokenPassthroughValue
  );
};

export const login = async ({ username, password, rememberMe = false }) => {
  const { token } = (await api.post('/session', { password, username })).data;
  setToken(rememberMe ? localStorage : sessionStorage, token);
  return token;
};

export const logout = () => {
  console.debug('removing token from local and session storage');
  clearToken();
};

export const check = async () => {
  if (!isLoggedIn()) {
    return false;
  }

  try {
    await api.get('/session');
    return true;
  } catch (error) {
    if (error.response.status === 401) {
      console.debug('session expired; clearing stored token');
      logout();
      return false;
    } else {
      throw error;
    }
  }
};

export const authHeaders = ({ csrf = false } = {}) => {
  const token = getToken();
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  const csrfToken = csrf ? getCsrfTokenFromCookieString() : null;
  if (csrfToken) {
    headers['X-CSRF-TOKEN'] = csrfToken;
  }

  return headers;
};
