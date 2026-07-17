// <copyright file="SearchList.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import SearchList from './SearchList';
import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('./SearchListRow', () => ({
  default: ({ search }) => (
    <tr>
      <td>{search.id}</td>
    </tr>
  ),
}));

describe('SearchList', () => {
  it('labels pagination actions', () => {
    const searches = Object.fromEntries(
      Array.from({ length: 101 }, (_, index) => [
        `search-${index}`,
        {
          id: `search-${index}`,
          startedAt: new Date(2026, 0, 1, 0, index).toISOString(),
        },
      ]),
    );

    render(<SearchList searches={searches} />);

    expect(
      screen.getByRole('button', { name: 'Previous search page' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Next search page' }),
    ).toBeInTheDocument();
  });
});
