import '@testing-library/jest-dom';
import AppContext from '../AppContext';
import TransfersHeader from './TransfersHeader';
import React from 'react';
import userEvent from '@testing-library/user-event';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

describe('TransfersHeader', () => {
  it('surfaces download filters on an empty Downloads view', () => {
    render(
      <AppContext.Provider
        value={{
          options: {
            filters: {
              download: {
                exclude: ['acapella', 'instrumental'],
              },
            },
            remoteConfiguration: true,
          },
        }}
      >
        <TransfersHeader
          direction="download"
          server={{ isConnected: true }}
          totalCount={0}
          transfers={[]}
        />
      </AppContext.Provider>,
    );

    const button = screen.getByRole('button', { name: 'Open download filters' });
    expect(button).toHaveTextContent('Download filters (2)');

    fireEvent.click(button);

    expect(screen.getByLabelText('Global download exclusions')).toHaveValue(
      'acapella\ninstrumental',
    );
  });

  it('explains bulk retry scope and peer traffic on both split controls', async () => {
    const user = userEvent.setup();
    const retryTooltip = 'Retry every eligible download in the chosen status group. This sends new requests to Soulseek peers; use the arrow menu to choose Errored, Cancelled, or All.';
    render(
      <AppContext.Provider value={{ options: {} }}>
        <TransfersHeader
          direction="download"
          onCancelAll={vi.fn()}
          onRemoveAll={vi.fn()}
          onRetryAll={vi.fn()}
          server={{ isConnected: true }}
          totalCount={1}
          transfers={[
            {
              directories: [{
                files: [{ direction: 'download', state: 'Completed, Errored' }],
              }],
            },
          ]}
        />
      </AppContext.Provider>,
    );

    const retry = screen.getByRole('button', { name: 'Retry All Errored' });
    await user.hover(retry);
    expect(await screen.findByText(retryTooltip)).toBeInTheDocument();

    const retryScope = document.querySelector(
      '.transfers-header-actions .ui.dropdown[aria-label="Choose downloads to retry"]',
    );
    expect(retryScope).toBeInTheDocument();
    await user.hover(retryScope);
    expect(await screen.findByText(retryTooltip)).toBeInTheDocument();
  });
});
