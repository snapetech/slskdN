// <copyright file="DirectoryTree.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import DirectoryTree from './DirectoryTree';
import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';
import userEvent from '@testing-library/user-event';

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

  it('explains folder selection and expands folders with native button controls', async () => {
    const user = userEvent.setup();
    const folder = {
      children: [{ children: [], fileCount: 0, locked: false, name: 'child' }],
      fileCount: 1,
      locked: false,
      name: 'folder',
    };
    const onSelect = vi.fn();
    const onDownload = vi.fn();
    const { container } = render(
      <DirectoryTree
        onDownload={onDownload}
        onSelect={onSelect}
        selectedDirectoryName=""
        tree={[folder]}
      />,
    );

    const selectFolder = screen.getByRole('button', { name: 'folder' });
    await user.hover(selectFolder);
    expect(await screen.findByText(/Select folder folder to view its contents/))
      .toBeInTheDocument();
    expect(onSelect).not.toHaveBeenCalled();

    await user.keyboard('{Tab}');
    const expand = screen.getByRole('button', { name: 'Expand folder' });
    expect(expand).toHaveAttribute('aria-expanded', 'false');
    await user.click(expand);
    expect(screen.getByRole('button', { name: 'Collapse folder' }))
      .toHaveAttribute('aria-expanded', 'true');
    expect(container).toHaveTextContent('child');
    expect(onDownload).not.toHaveBeenCalled();
  });
});
