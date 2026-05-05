// <copyright file="options.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as options from './options';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
    patch: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

describe('options', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('posts YAML validation bodies as JSON strings', async () => {
    api.post.mockResolvedValue({ data: null });

    await options.validateYaml({ yaml: 'web:\n  port: 5030\n' });

    expect(api.post).toHaveBeenCalledWith(
      '/options/yaml/validate',
      '"web:\\n  port: 5030\\n"',
    );
  });

  it('puts YAML update bodies as JSON strings', async () => {
    api.put.mockResolvedValue({ data: {} });

    await options.updateYaml({ yaml: 'remote_configuration: true\n' });

    expect(api.put).toHaveBeenCalledWith(
      '/options/yaml',
      '"remote_configuration: true\\n"',
    );
  });
});
