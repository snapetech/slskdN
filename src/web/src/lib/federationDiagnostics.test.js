// <copyright file="federationDiagnostics.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as federationDiagnostics from './federationDiagnostics';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
  },
}));

describe('federationDiagnostics', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('loads diagnostics from the versioned federation route', async () => {
    api.get.mockResolvedValue({ data: { warnings: [] } });

    await federationDiagnostics.getDiagnostics();

    expect(api.get).toHaveBeenCalledWith('/federation/diagnostics');
  });
});
