// <copyright file="SearchActionIcon.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import '@testing-library/jest-dom';
import React from 'react';
import SearchActionIcon from './SearchActionIcon';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

describe('SearchActionIcon', () => {
  it('explains stopping an active peer search before stopping it', async () => {
    const user = userEvent.setup();
    const onStop = vi.fn();
    render(
      <SearchActionIcon
        onRemove={vi.fn()}
        onStop={onStop}
        search={{ id: 'search-1', searchText: 'ambient', state: 'InProgress' }}
      />,
    );

    const stop = screen.getByRole('button', { name: 'Stop search ambient' });
    await user.hover(stop);
    expect(await screen.findByText(/no longer requests responses from Soulseek peers/))
      .toBeInTheDocument();
    expect(onStop).not.toHaveBeenCalled();

    await user.click(stop);
    expect(onStop).toHaveBeenCalledOnce();
  });

  it('explains deleting a completed search and its history', async () => {
    const user = userEvent.setup();
    const onRemove = vi.fn();
    render(
      <SearchActionIcon
        onRemove={onRemove}
        onStop={vi.fn()}
        search={{
          id: 'search-2',
          searchText: 'ambient',
          state: 'Completed, Succeeded',
        }}
      />,
    );

    await user.hover(screen.getByRole('button', {
      name: 'Delete completed search ambient',
    }));
    expect(await screen.findByText(/and its response history/)).toBeInTheDocument();
    expect(onRemove).not.toHaveBeenCalled();
  });
});
