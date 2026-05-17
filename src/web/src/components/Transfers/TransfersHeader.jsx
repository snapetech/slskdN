import {
  isStateCancellable,
  isStateRetryable,
  isStateSucceeded,
} from '../../lib/transfers';
import {
  applyHumanChallengeAutoResponse,
  canApplyHumanChallengeAutoResponse,
  getHumanChallengeAutoResponseEnabled,
  readStoredHumanChallengeAutoResponse,
  writeStoredHumanChallengeAutoResponse,
} from '../../lib/humanChallengeAutoResponse';
import AppContext from '../AppContext';
import { Div, Nbsp } from '../Shared';
import ShrinkableDropdownButton from '../Shared/ShrinkableDropdownButton';
import React, { useContext, useEffect, useMemo, useState } from 'react';
import { Checkbox, Icon, Popup, Segment } from 'semantic-ui-react';

const getRetryableFiles = ({ files, retryOption }) => {
  switch (retryOption) {
    case 'Errored':
      return files.filter((file) =>
        [
          'Completed, TimedOut',
          'Completed, Errored',
          'Completed, Rejected',
          'Completed, Aborted',
        ].includes(file.state),
      );
    case 'Cancelled':
      return files.filter((file) => file.state === 'Completed, Cancelled');
    case 'All':
      return files.filter((file) => isStateRetryable(file.state));
    default:
      return [];
  }
};

const getCancellableFiles = ({ cancelOption, files }) => {
  switch (cancelOption) {
    case 'All':
      return files.filter((file) => isStateCancellable(file.state));
    case 'Queued':
      return files.filter((file) =>
        ['Queued, Locally', 'Queued, Remotely'].includes(file.state),
      );
    case 'In Progress':
      return files.filter((file) => file.state === 'InProgress');
    default:
      return [];
  }
};

const getRemovableFiles = ({ files, removeOption }) => {
  switch (removeOption) {
    case 'Succeeded':
      return files.filter((file) => isStateSucceeded(file.state));
    case 'Errored':
      return files.filter((file) =>
        [
          'Completed, TimedOut',
          'Completed, Errored',
          'Completed, Rejected',
          'Completed, Aborted',
        ].includes(file.state),
      );
    case 'Cancelled':
      return files.filter((file) => file.state === 'Completed, Cancelled');
    case 'Completed':
      return files.filter((file) => isStateSucceeded(file.state));
    default:
      return [];
  }
};

const TransfersHeader = ({
  acceleratedEnabled = false,
  autoReplaceEnabled = false,
  autoReplaceThreshold = 5,
  cancelling = false,
  direction,
  hideCompleted = true,
  onAcceleratedChange,
  onAutoReplaceChange,
  onCancelAll,
  onHideCompletedChange,
  onRemoveAll,
  onRetryAll,
  removing = false,
  retrying = false,
  server = { isConnected: true },
  transfers,
}) => {
  const appContext = useContext(AppContext) || {};
  const options = appContext.options || {};
  const [removeOption, setRemoveOption] = useState('Succeeded');
  const [cancelOption, setCancelOption] = useState('All');
  const [retryOption, setRetryOption] = useState('Errored');
  const [humanCheckAutoResponseEnabled, setHumanCheckAutoResponseEnabled] =
    useState(() =>
      getHumanChallengeAutoResponseEnabled(options) ||
      readStoredHumanChallengeAutoResponse(),
    );
  const [humanCheckSaving, setHumanCheckSaving] = useState(false);
  const [humanCheckError, setHumanCheckError] = useState(false);
  const canApplyHumanCheck = canApplyHumanChallengeAutoResponse(options);

  useEffect(() => {
    setHumanCheckAutoResponseEnabled(
      getHumanChallengeAutoResponseEnabled(options) ||
        readStoredHumanChallengeAutoResponse(),
    );
  }, [options]);

  const files = useMemo(() => {
    return transfers
      .reduce((accumulator, username) => {
        const allUserFiles = username.directories.reduce(
          (directoryAccumulator, directory) => {
            return directoryAccumulator.concat(directory.files);
          },
          [],
        );

        return accumulator.concat(allUserFiles);
      }, [])
      .filter((file) => file.direction.toLowerCase() === direction);
  }, [direction, transfers]);

  const empty = files.length === 0;

  const setHumanCheckAutoResponse = async (enabled) => {
    setHumanCheckAutoResponseEnabled(enabled);
    writeStoredHumanChallengeAutoResponse(enabled);
    setHumanCheckError(false);

    if (!canApplyHumanCheck) {
      setHumanCheckError(true);
      return;
    }

    setHumanCheckSaving(true);
    try {
      await applyHumanChallengeAutoResponse(enabled);
    } catch {
      setHumanCheckError(true);
    } finally {
      setHumanCheckSaving(false);
    }
  };

  return (
    <Segment
      className="transfers-header-segment"
      raised
    >
      <div className="transfers-segment-icon">
        <Icon
          name={direction}
          size="big"
        />
      </div>
      <Div
        className="transfers-header-buttons"
        hidden={empty}
      >
        <ShrinkableDropdownButton
          color="green"
          disabled={retrying || empty || !server.isConnected}
          hidden={direction === 'upload'}
          icon="redo"
          loading={retrying}
          mediaQuery="(max-width: 715px)"
          onChange={(_, data) => setRetryOption(data.value)}
          onClick={() => onRetryAll(getRetryableFiles({ files, retryOption }))}
          options={[
            { key: 'errored', text: 'Errored', value: 'Errored' },
            { key: 'cancelled', text: 'Cancelled', value: 'Cancelled' },
            { key: 'all', text: 'All', value: 'All' },
          ]}
        >
          {`Retry ${retryOption === 'All' ? retryOption : `All ${retryOption}`}`}
        </ShrinkableDropdownButton>
        <Nbsp />
        <ShrinkableDropdownButton
          color="red"
          disabled={cancelling || empty}
          icon="x"
          loading={cancelling}
          mediaQuery="(max-width: 715px)"
          onChange={(_, data) => setCancelOption(data.value)}
          onClick={() =>
            onCancelAll(getCancellableFiles({ cancelOption, files }))
          }
          options={[
            { key: 'all', text: 'All', value: 'All' },
            { key: 'queued', text: 'Queued', value: 'Queued' },
            { key: 'inProgress', text: 'In Progress', value: 'In Progress' },
          ]}
        >
          {`Cancel ${cancelOption === 'All' ? cancelOption : `All ${cancelOption}`}`}
        </ShrinkableDropdownButton>
        <Nbsp />
        <ShrinkableDropdownButton
          disabled={removing || empty}
          icon="trash alternate"
          loading={removing}
          mediaQuery="(max-width: 715px)"
          onChange={(_, data) => setRemoveOption(data.value)}
          onClick={() =>
            onRemoveAll(
              getRemovableFiles({ files, removeOption }),
              false,
              { useBulkClear: removeOption === 'Completed' },
            )
          }
          options={[
            { key: 'succeeded', text: 'Succeeded', value: 'Succeeded' },
            { key: 'errored', text: 'Errored', value: 'Errored' },
            { key: 'cancelled', text: 'Cancelled', value: 'Cancelled' },
            { key: 'completed', text: 'Complete', value: 'Completed' },
          ]}
        >
          {`Remove All ${removeOption === 'Completed' ? 'Complete' : removeOption}`}
        </ShrinkableDropdownButton>
        <Nbsp />
        <Popup
          content="Allow slow or stalled downloads to use verified alternate sources. Soulseek peers use sequential failover; true multipart chunking is reserved for trusted slskdN mesh peers."
          position="bottom right"
          trigger={
            <Checkbox
              checked={acceleratedEnabled}
              className="accelerated-downloads-toggle"
              disabled={!server.isConnected}
              hidden={direction === 'upload'}
              label="Accelerated"
              onChange={(_, data) => onAcceleratedChange?.(data.checked)}
              toggle
            />
          }
        />
        <Nbsp />
        <Popup
          content={`Automatically find and replace stuck downloads with alternative sources. Replacements must be within ${autoReplaceThreshold}% file size.`}
          position="bottom right"
          trigger={
            <Checkbox
              checked={autoReplaceEnabled}
              className="auto-replace-toggle"
              disabled={!server.isConnected}
              hidden={direction === 'upload'}
              label="Auto-Replace"
              onChange={(_, data) => onAutoReplaceChange?.(data.checked)}
              toggle
            />
          }
        />
        <Nbsp />
        <Popup
          content={
            canApplyHumanCheck
              ? 'Automatically answer private messages that look like human-check or share-gate prompts. This helps with peer gates before or during downloads.'
              : 'Remember this preference in the browser. Enable remote configuration to apply it to the daemon so replies are sent even when you are not on this pane.'
          }
          position="bottom right"
          trigger={
            <Checkbox
              checked={humanCheckAutoResponseEnabled}
              className="human-check-auto-response-toggle"
              disabled={humanCheckSaving}
              hidden={direction === 'upload'}
              label={humanCheckError ? 'Auto Reply (local)' : 'Auto Reply'}
              onChange={(_, data) => setHumanCheckAutoResponse(data.checked)}
              toggle
            />
          }
        />
        <Nbsp />
        <Popup
          content="Hide successful 100% transfers from the list. Failed, aborted, timed-out, and rejected downloads stay visible for retry or review."
          position="bottom right"
          trigger={
            <Checkbox
              checked={hideCompleted}
              className="hide-completed-toggle"
              label="Hide Complete"
              onChange={(_, data) => onHideCompletedChange?.(data.checked)}
              toggle
            />
          }
        />
      </Div>
    </Segment>
  );
};

export default TransfersHeader;
