// <copyright file="DirectoryTree.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import DirectoryTree from './DirectoryTree';
import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';

describe('DirectoryTree', () => {
  beforeEach(() => {
    vi.stubGlobal('ResizeObserver', class {
      observe() {}

      unobserve() {}

      disconnect() {}
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('windows large directory trees without truncating the visible folder list', () => {
    const tree = Array.from({ length: 2505 }, (_, index) => ({
      children: [],
      fileCount: 0,
      locked: false,
      name: `folder-${index}`,
    }));
    const { container } = render(
      <DirectoryTree
        onDownload={vi.fn()}
        onSelect={vi.fn()}
        selectedDirectoryName=""
        tree={tree}
      />,
    );

    expect(screen.getByText('folder-0')).toBeInTheDocument();
    expect(screen.queryByText('Showing the first 2000 visible folders')).not.toBeInTheDocument();

    const list = container.querySelector('[role="list"]');
    expect(list).toBeInTheDocument();
    expect(list.children.length).toBeLessThan(40);

    list.scrollTop = 36 * 2400;
    fireEvent.scroll(list);

    expect(screen.getByText('folder-2400')).toBeInTheDocument();
  });
});
