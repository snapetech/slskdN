import '@testing-library/jest-dom';
import AppContext from '../AppContext';
import TransfersHeader from './TransfersHeader';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

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
});
