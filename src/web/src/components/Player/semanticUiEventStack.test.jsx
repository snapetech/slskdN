import React from 'react';
import { cleanup, render } from '@testing-library/react';
import { Modal } from 'semantic-ui-react';
import eventStack from 'semantic-ui-react/dist/es/lib/eventStack';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

describe('Semantic UI event-stack target cleanup', () => {
  it('unsubscribes from the modal DOM target after its React ref is cleared', () => {
    const subscribe = vi.spyOn(eventStack, 'sub');
    const unsubscribe = vi.spyOn(eventStack, 'unsub');
    const view = render(<Modal open><div>Queue content</div></Modal>);
    const target = subscribe.mock.calls
      .map(([, , options]) => options?.target)
      .find((candidate) => candidate instanceof HTMLElement &&
        candidate.classList.contains('page') && candidate.classList.contains('modals'));
    expect(target).toBeDefined();

    view.unmount();

    expect(unsubscribe.mock.calls.some(([name, , options]) =>
      (name === 'mouseenter' || name === 'mouseleave') && options?.target === target)).toBe(true);
  });
});
