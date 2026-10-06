// <copyright file="MediaCorePods.test.jsx" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import * as mediacore from '../../../lib/mediacore';
import MediaCorePods from './MediaCorePods';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { vi } from 'vitest';

vi.mock('../../../lib/mediacore', () => ({ publishPod: vi.fn() }));

describe('MediaCore Pod action guidance', () => {
  it('explains DHT visibility and keeps the tooltip reachable while publish is disabled', async () => {
    render(
      <MediaCorePods
        isPodWorkflowVisible={() => true}
        supportedAlgorithms={[]}
      />,
    );

    fireEvent.click(screen.getByText('Advanced DHT publishing controls'));

    const publish = screen.getByRole('button', { name: 'Publish Pod' });
    expect(publish).toBeDisabled();
    fireEvent.mouseEnter(publish.parentElement);
    expect(
      await screen.findByText(
        'Publish this pod metadata to the DHT so mesh participants can discover it. Review its visibility, tags, and focus content before publishing.',
      ),
    ).toBeInTheDocument();
    expect(mediacore.publishPod).not.toHaveBeenCalled();
  });
});
