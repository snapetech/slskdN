// <copyright file="session.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as session from './session';
import { setToken } from './token';

vi.mock('./api', async () => {
  const actual = await vi.importActual('./api');
  return {
    ...actual,
    default: {
      get: vi.fn(),
      post: vi.fn(),
    },
  };
});

describe('session', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    Object.defineProperty(document, 'cookie', {
      configurable: true,
      value: 'XSRF-TOKEN=csrf-token',
      writable: true,
    });
  });

  it('adds Authorization when a bearer token is stored', () => {
    setToken(sessionStorage, 'jwt-token');

    expect(session.authHeaders()).toEqual({
      Authorization: 'Bearer jwt-token',
    });
  });

  it('adds CSRF only when requested for direct fetch mutations', () => {
    expect(session.authHeaders()).toEqual({});
    expect(session.authHeaders({ csrf: true })).toEqual({
      'X-CSRF-TOKEN': 'csrf-token',
    });
  });
});
