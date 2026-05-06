import '@testing-library/jest-dom';
import DiscoveryGraphAtlasPanel from './DiscoveryGraphAtlasPanel';
import * as discoveryGraph from '../../lib/discoveryGraph';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { MemoryRouter, useNavigate } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('./DiscoveryGraphAtlas', () => ({
  default: ({ graph }) => <div data-testid="atlas-graph">{graph?.title}</div>,
}));

vi.mock('react-toastify', () => ({
  toast: {
    error: vi.fn(),
  },
}));

vi.mock('../../lib/discoveryGraph', async (importOriginal) => {
  const actual = await importOriginal();
  return {
    ...actual,
    buildDiscoveryGraph: vi.fn(async (request) => ({
      edges: [],
      nodes: [],
      title: request.title,
    })),
  };
});

const Harness = () => {
  const navigate = useNavigate();
  return (
    <>
      <button
        type="button"
        onClick={() => navigate('/discovery-graph?scope=track&title=Second')}
      >
        Open Second
      </button>
      <DiscoveryGraphAtlasPanel persistRoute />
    </>
  );
};

describe('DiscoveryGraphAtlasPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('rebuilds the graph when the mounted route query changes', async () => {
    render(
      <MemoryRouter initialEntries={['/discovery-graph?scope=track&title=First']}>
        <Harness />
      </MemoryRouter>,
    );

    await waitFor(() =>
      expect(discoveryGraph.buildDiscoveryGraph).toHaveBeenCalledWith(
        expect.objectContaining({ title: 'First' }),
      ));

    fireEvent.click(screen.getByText('Open Second'));

    await waitFor(() =>
      expect(discoveryGraph.buildDiscoveryGraph).toHaveBeenCalledWith(
        expect.objectContaining({ title: 'Second' }),
      ));
    expect(await screen.findByTestId('atlas-graph')).toHaveTextContent('Second');
  });
});
